namespace Smart_Medc.Application.Common
{
    public class FileUploadResult
    {
        public string StoragePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
