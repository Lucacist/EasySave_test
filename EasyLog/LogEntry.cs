namespace EasyLog;

public class LogEntry
{
    public string Timestamp { get; set; }
    public string BackupName  { get; set; }
    public string SourceFilePath { get; set; }
    public string TargetFilePath { get; set; }
    public long FileSize { get; set; }
    public long FileTransferTime  { get; set; }
}