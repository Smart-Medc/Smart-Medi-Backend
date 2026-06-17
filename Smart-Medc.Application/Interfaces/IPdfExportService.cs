using Smart_Medc.Application.DTOs.Patient;

namespace Smart_Medc.Application.Interfaces
{
    public interface IPdfExportService
    {
        Task<byte[]> GenerateMedicalRecordsPdfAsync(
            MedicalRecordsExportDto exportData,
            string? fileName = null,
            CancellationToken cancellationToken = default);
    }
}
