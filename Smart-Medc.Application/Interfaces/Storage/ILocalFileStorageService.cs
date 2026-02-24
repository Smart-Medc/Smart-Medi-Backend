using Microsoft.AspNetCore.Http;

namespace Smart_Medc.Application.Interfaces.Storage
{
    public interface ILocalFileStorageService
    {
        Task<(bool Success, string? FilePath, string? ErrorMessage)> UploadFileAsync(
            IFormFile file,
            string folder,
            string? customFileName = null);

        Task<bool> DeleteFileAsync(string filePath);

        string GetFileUrl(string filePath);


        Task<bool> FileExistsAsync(string filePath);

        (bool IsValid, string? ErrorMessage) ValidateFile(
            IFormFile file,
            string[] allowedExtensions,
            long maxSizeInBytes);
    }
}