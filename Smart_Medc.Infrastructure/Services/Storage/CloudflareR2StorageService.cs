using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.Infrastructure.Services.Storage
{
    public class CloudflareR2StorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<CloudflareR2StorageService> _logger;

        public CloudflareR2StorageService(
            IAmazonS3 s3Client,
            IConfiguration configuration,
            ILogger<CloudflareR2StorageService> logger)
        {
            _s3Client = s3Client;
            _bucketName = configuration["CloudflareR2:BucketName"]
                ?? throw new ArgumentNullException("CloudflareR2:BucketName is not configured");
            _logger = logger;
        }

        public async Task<FileUploadResult> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string containerName,
            CancellationToken cancellationToken = default)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required", nameof(fileName));
            if (string.IsNullOrWhiteSpace(contentType)) contentType = "application/octet-stream";
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException("Container name is required", nameof(containerName));

            var safeFileName = Path.GetFileName(fileName);
            var storagePath = $"{containerName}/{Guid.NewGuid()}/{safeFileName}";

            // CRITICAL:
            // Copy caller stream into an owned in-memory stream so we:
            // 1) do not depend on caller stream lifetime,
            // 2) avoid seek/position issues,
            // 3) avoid side effects on caller stream used elsewhere.
            await using var ownedStream = new MemoryStream();

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            await fileStream.CopyToAsync(ownedStream, cancellationToken);
            ownedStream.Position = 0;

            var contentLength = ownedStream.Length;

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = storagePath,
                InputStream = ownedStream,
                ContentType = contentType,
                AutoCloseStream = false, // explicit: do not close owned stream before request fully completes
                UseChunkEncoding = false
            };
            putRequest.Headers.ContentLength = contentLength;

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            _logger.LogInformation(
                "File uploaded: {StoragePath}, Size: {Size}, Bucket: {Bucket}",
                storagePath, contentLength, _bucketName);

            return new FileUploadResult
            {
                StoragePath = storagePath,
                FileName = safeFileName,
                FileSizeBytes = contentLength,
                ContentType = contentType
            };
        }

        public async Task<Stream> DownloadFileAsync(
            string containerName,
            string storagePath,
            CancellationToken cancellationToken = default)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = storagePath
            };

            using var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<bool> DeleteFileAsync(
            string containerName,
            string storagePath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = storagePath
                };

                await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
                _logger.LogInformation("File deleted: {StoragePath}", storagePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {StoragePath}", storagePath);
                return false;
            }
        }

        public Task<string> GeneratePresignedUrlAsync(
            string containerName,
            string storagePath,
            TimeSpan validity,
            CancellationToken cancellationToken = default)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = storagePath,
                Expires = DateTime.UtcNow.Add(validity),
                Verb = HttpVerb.GET
            };

            var url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }
    }
}