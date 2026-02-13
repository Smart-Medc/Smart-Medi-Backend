using Smart_Medc.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Uploads a file to cloud storage
        /// </summary>
        Task<FileUploadResult> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string containerName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file from cloud storage
        /// </summary>
        Task<Stream> DownloadFileAsync(
            string containerName,
            string storagePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file from cloud storage
        /// </summary>
        Task<bool> DeleteFileAsync(
            string containerName,
            string storagePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a pre-signed URL for temporary access
        /// </summary>
        Task<string> GeneratePresignedUrlAsync(
            string containerName,
            string storagePath,
            TimeSpan validity,
            CancellationToken cancellationToken = default);
    }

}
