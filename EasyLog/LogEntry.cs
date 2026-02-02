namespace EasyLog;

public class LogEntry
{
    // Initialisation par défaut pour éviter les null
    public string Timestamp { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public string BackupName { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty; // Format UNC
    public string TargetFilePath { get; set; } = string.Empty; // Format UNC
    public long FileSize { get; set; }
    public long FileTransferTime { get; set; } // ms (négatif si erreur)
}