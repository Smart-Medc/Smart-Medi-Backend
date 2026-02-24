using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.DTOs.Auth;
using Smart_Medc.Application.Interfaces.Storage;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.Services
{
    public class OrganizationDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<OrganizationDocumentService> _logger;

        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        private const long _maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public OrganizationDocumentService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorage,
            ILogger<OrganizationDocumentService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        /// <summary>
        /// Can upload multiple documents for an organization during registration
        /// </summary>
        public async Task<List<OrganizationDocument>> UploadDocumentsDuringRegistrationAsync(
            Guid organizationId,
            List<IFormFile> files,
            List<OrganizationDocumentType>? documentTypes,
            List<string>? documentNames)
        {
            var uploadedDocuments = new List<OrganizationDocument>();

            try
            {
                if (files == null || !files.Any())
                {
                    _logger.LogWarning("No files provided for organization {OrganizationId}", organizationId);
                    return uploadedDocuments;
                }

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];

                    var validation = _fileStorage.ValidateFile(file, _allowedExtensions, _maxFileSizeBytes);
                    if (!validation.IsValid)
                    {
                        _logger.LogWarning(
                            "Invalid file during organization registration: {FileName}, Error: {Error}",
                            file.FileName,
                            validation.ErrorMessage);
                        continue;
                    }

                    // Parse document type
                    var docType = documentTypes != null && i < documentTypes.Count
                         ? documentTypes[i]
                         : OrganizationDocumentType.Other;

                    var docName = documentNames != null && i < documentNames.Count
                        ? documentNames[i]
                        : Path.GetFileNameWithoutExtension(file.FileName);

                    var folder = $"organizations/{organizationId}/documents";
                    var uploadResult = await _fileStorage.UploadFileAsync(file, folder);

                    if (!uploadResult.Success)
                    {
                        _logger.LogWarning(
                            "Failed to upload file during registration: {FileName}, Error: {Error}",
                            file.FileName,
                            uploadResult.ErrorMessage);
                        continue;
                    }

                    var document = new OrganizationDocument
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organizationId,
                        DocumentName = docName,
                        DocumentType = docType,
                        FileName = file.FileName,
                        StoragePath = uploadResult.FilePath!,
                        ContentType = file.ContentType,
                        FileSizeBytes = file.Length,
                        VerificationStatus = DocumentVerificationStatus.Pending,
                        UploadedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.OrganizationDocuments.AddAsync(document);
                    uploadedDocuments.Add(document);

                    _logger.LogInformation(
                        "Document uploaded successfully for organization {OrganizationId}: {DocumentName} , {DocumentType}",
                        organizationId,
                        docName,
                        docType);
                }

                if (uploadedDocuments.Any())
                {
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation(
                        "Uploaded {Count} documents for organization {OrganizationId}",
                        uploadedDocuments.Count,
                        organizationId);
                }

                return uploadedDocuments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading documents for organization {OrganizationId}", organizationId);

                foreach (var doc in uploadedDocuments)
                {
                    try
                    {
                        await _fileStorage.DeleteFileAsync(doc.StoragePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Error cleaning up file: {FilePath}", doc.StoragePath);
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// Get all documents for an organization
        /// </summary>
        public async Task<List<DocumentDto>> GetOrganizationDocumentsAsync(Guid organizationId)
        {
            var documents = await _unitOfWork.OrganizationDocuments.FindAsync(
                d => d.OrganizationId == organizationId);

            return documents
                .OrderByDescending(d => d.UploadedAt)
                .Select(MapToDto)
                .ToList();
        }

        private DocumentDto MapToDto(OrganizationDocument document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                OrganizationId = document.OrganizationId,
                DocumentName = document.DocumentName,
                DocumentType = document.DocumentType.ToString(),
                FileName = document.FileName,
                FileUrl = _fileStorage.GetFileUrl(document.StoragePath),
                ContentType = document.ContentType,
                FileSizeBytes = document.FileSizeBytes,
                VerificationStatus = document.VerificationStatus.ToString(),
                UploadedAt = document.UploadedAt,
                VerifiedAt = document.VerifiedAt
            };
        }
    }
}