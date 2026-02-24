using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Interfaces.Storage;

namespace Smart_Medc.Application.Services.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _uploadsFolder;

        public LocalFileStorageService(
            IWebHostEnvironment environment,
            ILogger<LocalFileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;

            // wwwroot/uploads
            _uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");

            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
                _logger.LogInformation("Created uploads folder at: {Path}", _uploadsFolder);
            }
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> UploadFileAsync(
            IFormFile file,
            string folder,
            string? customFileName = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return (false, null, "File is empty");
                }

                // uploads/{folder}/
                var folderPath = Path.Combine(_uploadsFolder, folder);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Generate unique filename
                var fileName = customFileName ?? GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = Path.Combine(folder, fileName).Replace("\\", "/");

                _logger.LogInformation("File uploaded successfully: {FilePath}", relativePath);

                return (true, relativePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return (false, null, $"Failed to upload file: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_uploadsFolder, filePath);

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    return true;
                }

                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public string GetFileUrl(string filePath)
        {
            // Return URL path: /uploads/{folder}/{filename}
            return $"/uploads/{filePath}";
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            var fullPath = Path.Combine(_uploadsFolder, filePath);
            return await Task.FromResult(File.Exists(fullPath));
        }

        public (bool IsValid, string? ErrorMessage) ValidateFile(
            IFormFile file,
            string[] allowedExtensions,
            long maxSizeInBytes)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "File is empty");
            }

            if (file.Length > maxSizeInBytes)
            {
                var maxSizeMB = maxSizeInBytes / 1024.0 / 1024.0;
                return (false, $"File size exceeds maximum allowed size of {maxSizeMB:F2} MB");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return (false, $"File type {extension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            if (file.FileName.Contains('\0'))
            {
                return (false, "Invalid filename");
            }

            return (true, null);
        }

        private static string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

            fileNameWithoutExtension = string.Concat(fileNameWithoutExtension.Split(Path.GetInvalidFileNameChars()));

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];

            return $"{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
        }
    }
}