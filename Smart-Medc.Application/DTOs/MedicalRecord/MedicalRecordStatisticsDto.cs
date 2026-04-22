namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class MedicalRecordStatisticsDto
    {
        public int TotalRecords { get; set; }
        public int LabReports { get; set; }
        public int Imaging { get; set; }
        public int ConsultationNotes { get; set; }
        public int Immunizations { get; set; }
        public int Other { get; set; }
        public long StorageUsedBytes { get; set; }
        public long StorageLimitBytes { get; set; }
        public double StorageUsedPercentage { get; set; }
    }
}
