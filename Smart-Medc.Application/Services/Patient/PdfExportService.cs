using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.DTOs.Patient;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.Application.Services.Patient
{
    public class PdfExportService : IPdfExportService
    {
        private readonly ILogger<PdfExportService> _logger;
        private readonly PdfFont _boldFont;

        public PdfExportService(ILogger<PdfExportService> logger)
        {
            _logger = logger;
            _boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        }

        public async Task<byte[]> GenerateMedicalRecordsPdfAsync(
            MedicalRecordsExportDto exportData,
            string? fileName = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    var writer = new PdfWriter(memoryStream);
                    var pdfDoc = new PdfDocument(writer);
                    var document = new Document(pdfDoc);

                    document.Add(new Paragraph("Medical Records Export")
                        .SetFontSize(24)
                        .SetFont(_boldFont)
                        .SetMarginBottom(20));

                    // Metadata
                    var metadataTable = new Table(2);
                    metadataTable.AddCell("Export Date:");
                    metadataTable.AddCell(exportData.Metadata.ExportedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    metadataTable.AddCell("Format:");
                    metadataTable.AddCell(exportData.Metadata.ExportFormat.ToUpper());
                    metadataTable.AddCell("Version:");
                    metadataTable.AddCell(exportData.Metadata.Version);
                    document.Add(metadataTable);
                    document.Add(new Paragraph("\n"));

                    // Patient Info
                    if (exportData.PatientInfo != null)
                    {
                        document.Add(new Paragraph("Patient Information")
                            .SetFontSize(16)
                            .SetFont(_boldFont)
                            .SetMarginTop(20)
                            .SetMarginBottom(10));

                        var patientTable = new Table(2);
                        patientTable.AddCell("Full Name:").AddCell(exportData.PatientInfo.FullName);
                        patientTable.AddCell("Email:").AddCell(exportData.PatientInfo.Email);
                        patientTable.AddCell("Phone:").AddCell(exportData.PatientInfo.PhoneNumber ?? "N/A");
                        patientTable.AddCell("Date of Birth:").AddCell(exportData.PatientInfo.DateOfBirth.ToString("yyyy-MM-dd"));
                        patientTable.AddCell("Age:").AddCell(exportData.PatientInfo.Age.ToString());
                        patientTable.AddCell("Gender:").AddCell(exportData.PatientInfo.Gender);
                        patientTable.AddCell("Blood Type:").AddCell(exportData.PatientInfo.BloodType ?? "N/A");
                        patientTable.AddCell("Address:").AddCell(exportData.PatientInfo.Address ?? "N/A");
                        patientTable.AddCell("Allergies:").AddCell(
                            exportData.PatientInfo.HasNoKnownAllergies ? "No known allergies" :
                            exportData.PatientInfo.Allergies ?? "Not specified");
                        document.Add(patientTable);
                        document.Add(new Paragraph("\n"));

                        // Emergency Contact
                        document.Add(new Paragraph("Emergency Contact")
                            .SetFontSize(14)
                            .SetFont(_boldFont)
                            .SetMarginTop(15)
                            .SetMarginBottom(10));

                        var emergencyTable = new Table(2);
                        emergencyTable.AddCell("Name:").AddCell(exportData.PatientInfo.EmergencyContactName);
                        emergencyTable.AddCell("Phone:").AddCell(exportData.PatientInfo.EmergencyContactPhone);
                        emergencyTable.AddCell("Relationship:").AddCell(exportData.PatientInfo.EmergencyContactRelationship);
                        document.Add(emergencyTable);
                        document.Add(new Paragraph("\n"));
                    }

                    // Medical Records
                    if (exportData.MedicalRecords.Any())
                    {
                        document.Add(new Paragraph($"Medical Records ({exportData.MedicalRecords.Count})")
                            .SetFontSize(16)
                            .SetFont(_boldFont)
                            .SetMarginTop(20)
                            .SetMarginBottom(10));

                        foreach (var record in exportData.MedicalRecords)
                        {
                            document.Add(new Paragraph($"• {record.Title}")
                                .SetFontSize(12)
                                .SetFont(_boldFont));

                            var recordTable = new Table(2);
                            recordTable.AddCell("Record Type:").AddCell(record.RecordType);
                            recordTable.AddCell("Date:").AddCell(record.RecordDate.ToString("yyyy-MM-dd"));
                            recordTable.AddCell("Provider:").AddCell(record.ProviderName ?? "N/A");
                            recordTable.AddCell("Status:").AddCell(record.Status);
                            if (!string.IsNullOrEmpty(record.FindingsSummary))
                            {
                                recordTable.AddCell("Findings:").AddCell(record.FindingsSummary);
                            }
                            recordTable.AddCell("Documents:").AddCell(record.Documents.Count.ToString());
                            document.Add(recordTable);
                            document.Add(new Paragraph("\n"));
                        }
                    }

                    // Medications
                    if (exportData.Medications.Any())
                    {
                        document.Add(new Paragraph($"Medications ({exportData.Medications.Count})")
                            .SetFontSize(16)
                            .SetFont(_boldFont)
                            .SetMarginTop(20)
                            .SetMarginBottom(10));

                        foreach (var med in exportData.Medications)
                        {
                            var statusBadge = med.IsActive ? "ACTIVE" : "INACTIVE";
                            document.Add(new Paragraph($"• {med.Name} ({statusBadge})")
                                .SetFontSize(12)
                                .SetFont(_boldFont));

                            var medTable = new Table(2);
                            medTable.AddCell("Dosage:").AddCell(med.Dosage);
                            medTable.AddCell("Frequency:").AddCell(med.Frequency);
                            medTable.AddCell("Route:").AddCell(med.Route);
                            medTable.AddCell("Start Date:").AddCell(med.StartDate.ToString("yyyy-MM-dd"));
                            if (med.EndDate.HasValue)
                            {
                                medTable.AddCell("End Date:").AddCell(med.EndDate.Value.ToString("yyyy-MM-dd"));
                            }
                            if (!string.IsNullOrEmpty(med.PrescribingDoctor))
                            {
                                medTable.AddCell("Doctor:").AddCell(med.PrescribingDoctor);
                            }
                            document.Add(medTable);
                            document.Add(new Paragraph("\n"));
                        }
                    }

                    // Summary
                    if (exportData.Summary != null)
                    {
                        document.Add(new Paragraph("Summary")
                            .SetFontSize(16)
                            .SetFont(_boldFont)
                            .SetMarginTop(20)
                            .SetMarginBottom(10));

                        var summaryTable = new Table(2);
                        summaryTable.AddCell("Total Medical Records:").AddCell(exportData.Summary.TotalMedicalRecords.ToString());
                        summaryTable.AddCell("Total Documents:").AddCell(exportData.Summary.TotalDocuments.ToString());
                        summaryTable.AddCell("Active Medications:").AddCell(exportData.Summary.ActiveMedications.ToString());
                        summaryTable.AddCell("Total Medications:").AddCell(exportData.Summary.TotalMedications.ToString());
                        summaryTable.AddCell("Date Range:").AddCell($"{exportData.Summary.DateRangeFrom} to {exportData.Summary.DateRangeTo}");
                        document.Add(summaryTable);
                    }

                    document.Add(new Paragraph("\n\n"));
                    document.Add(new Paragraph("This is a confidential medical document. Please keep it secure.")
                        .SetFontSize(9)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE))
                        .SetTextAlignment(TextAlignment.CENTER));

                    document.Close();

                    var pdfBytes = memoryStream.ToArray();
                    _logger.LogInformation("PDF generated successfully. Size: {Size} bytes", pdfBytes.Length);

                    return await Task.FromResult(pdfBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF export");
                throw;
            }
        }
    }
}