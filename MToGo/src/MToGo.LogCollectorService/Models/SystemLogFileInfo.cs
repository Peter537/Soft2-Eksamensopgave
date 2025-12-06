namespace MToGo.LogCollectorService.Models
{
    public class SystemLogFileInfo
    {
        public string Date { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}
