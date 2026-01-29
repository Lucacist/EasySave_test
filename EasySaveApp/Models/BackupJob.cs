using System.Diagnostics;
using EasySaveApp.Strategies;

namespace EasySaveApp.Models;

// La classe pour transporter les données de l'événement
public class ProgressEventArgs : EventArgs
{
    public BackupJob Job { get; set; } = null!;
}

public class BackupJob
{
    public string Name { get; set; }
    public string SourceDirectory { get; set; }
    public string TargetDirectory { get; set; }
    public BackupType Type { get; set; }
    public string State { get; set; } 
    public long TotalFilesSize { get; set; }
    public long TotalFilesCount { get; set; }
    public long FilesItemsLeft { get; set; }
    public long FilesSizeLeft { get; set; }       // Taille restante exigée
    public int Progress { get; set; }
    public string CurrentSourceFile { get; set; } = string.Empty;
    public string CurrentTargetFile { get; set; } = string.Empty;
    public string LastActionTimestamp { get; set; } = string.Empty;
    
    // L'événement auquel le JobService va s'abonner
    public event EventHandler<ProgressEventArgs>? OnProgress;
    
    // Constructeur parameterless pour la désérialisation JSON
    public BackupJob()
    {
        Name = string.Empty;
        SourceDirectory = string.Empty;
        TargetDirectory = string.Empty;
        Type = BackupType.Full;
        State = "Idle";
    }
    
    public BackupJob(string name, string source, string target, BackupType type) 
    { 
        Name = name; 
        SourceDirectory = source; 
        TargetDirectory = target; 
        Type = type; 
        State = "Idle";
    }

    public void Execute()
    {
        if (Directory.Exists(SourceDirectory))
        {
            CalculateStatistics(SourceDirectory);
            if (TotalFilesCount == 0) return;

            State = "Active";
            IBackupStrategy strategy = BackupStrategyFactory.GetStrategy(this.Type);

            CopyAll(SourceDirectory, TargetDirectory, strategy);
        
            State = "Idle";
            Progress = 100;
            NotifyProgress(); // Alerte de fin
        }
    }
    private void CopyAll(string sourcePath, string targetPath, IBackupStrategy strategy)
    {
        Directory.CreateDirectory(targetPath);
        EasyLog.Logger logger = new EasyLog.Logger();

        foreach (string filePath in Directory.GetFiles(sourcePath))
        {
            string fileName = Path.GetFileName(filePath);
            string destFile = Path.Combine(targetPath, fileName);
            FileInfo sourceFile = new FileInfo(filePath);
            FileInfo targetFile = new FileInfo(destFile);
            
            this.CurrentSourceFile = filePath;
            this.CurrentTargetFile = destFile;
            this.LastActionTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // Utilisation du Polymorphisme
            if (strategy.ShouldCopy(sourceFile, targetFile))
            {
                Stopwatch sw = Stopwatch.StartNew();
                try 
                {
                    File.Copy(filePath, destFile, true);
                    sw.Stop();

                    logger.WriteLog(new EasyLog.LogEntry {
                        Timestamp = this.LastActionTimestamp,
                        BackupName = this.Name,
                        // Utilisation du format UNC ici
                        SourceFilePath = ToUNCPath(filePath), 
                        TargetFilePath = ToUNCPath(destFile),
                        FileSize = sourceFile.Length,
                        FileTransferTime = sw.ElapsedMilliseconds
                    });
                }
                catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
            }

            // Mise à jour des compteurs et du state.json
            this.FilesItemsLeft--;
            this.FilesSizeLeft -= sourceFile.Length;
            if (TotalFilesCount > 0)
                this.Progress = (int)((1 - (double)FilesItemsLeft / TotalFilesCount) * 100);
            
            // ÉTAPE OBSERVER : On "crie" que le travail a avancé
            NotifyProgress();        
        }

        foreach (string directoryPath in Directory.GetDirectories(sourcePath))
        {
            string destDirectory = Path.Combine(targetPath, Path.GetFileName(directoryPath));
            CopyAll(directoryPath, destDirectory, strategy);
        }
    }
    
    private string ToUNCPath(string path)
    {
        // Si c'est déjà un chemin réseau, on ne touche à rien
        if (path.StartsWith(@"\\")) return path;
    
        // Sinon, on simule le format UNC : C:\ devient \\localhost\C$\
        return path.Replace(":", "$").Insert(0, @"\\localhost\");
    }
    
    private void NotifyProgress()
    {
        // On déclenche l'événement pour prévenir les abonnés
        OnProgress?.Invoke(this, new ProgressEventArgs { Job = this });
    }
    
    private void CalculateStatistics(string sourcePath)
    {
        var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
        this.TotalFilesCount = files.Length;
        this.FilesItemsLeft = files.Length;
        this.TotalFilesSize = 0;
        foreach (var file in files)
        {
            this.TotalFilesSize += new FileInfo(file).Length;
        }
        this.FilesSizeLeft = this.TotalFilesSize; // Initialise la taille restante
    }
}