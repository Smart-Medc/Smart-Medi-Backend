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
            var storagePath = $"{containerName}/{Guid.NewGuid()}/{fileName}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = storagePath,
                InputStream = fileStream,
                ContentType = contentType,
                UseChunkEncoding = false
            };
            putRequest.Headers.ContentLength = fileStream.Length;

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            _logger.LogInformation("File uploaded: {StoragePath}, Size: {Size}", storagePath, fileStream.Length);

            return new FileUploadResult
            {
                StoragePath = storagePath,
                FileName = fileName,
                FileSizeBytes = fileStream.Length,
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

            var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);

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
