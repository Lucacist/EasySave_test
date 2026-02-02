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
    
    // Logger partagé pour tous les fichiers du job (évite de créer une instance à chaque fichier)
    private readonly EasyLog.Logger _logger;
    
    // Constructeur parameterless pour la désérialisation JSON
    public BackupJob()
    {
        Name = string.Empty;
        SourceDirectory = string.Empty;
        TargetDirectory = string.Empty;
        Type = BackupType.Full;
        State = "Idle";
        _logger = new EasyLog.Logger();
    }
    
    public BackupJob(string name, string source, string target, BackupType type) 
    { 
        Name = name; 
        SourceDirectory = source; 
        TargetDirectory = target; 
        Type = type; 
        State = "Idle";
        _logger = new EasyLog.Logger();
    }

    public void Execute()
    {
        // Validation des chemins
        if (string.IsNullOrWhiteSpace(SourceDirectory))
        {
            Console.WriteLine($"Error: Source directory is empty for job '{Name}'");
            return;
        }
        
        if (!Directory.Exists(SourceDirectory))
        {
            Console.WriteLine($"Error: Source directory does not exist: {SourceDirectory}");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(TargetDirectory))
        {
            Console.WriteLine($"Error: Target directory is empty for job '{Name}'");
            return;
        }
        
        CalculateStatistics(SourceDirectory);
        if (TotalFilesCount == 0) return;

        State = "Active";
        IBackupStrategy strategy = BackupStrategyFactory.GetStrategy(this.Type);

        CopyAll(SourceDirectory, TargetDirectory, strategy);
    
        State = "Idle";
        Progress = 100;
        NotifyProgress(); // Alerte de fin
    }
    private void CopyAll(string sourcePath, string targetPath, IBackupStrategy strategy)
    {
        Directory.CreateDirectory(targetPath);

        foreach (string filePath in Directory.GetFiles(sourcePath))
        {
            string fileName = Path.GetFileName(filePath);
            string destFile = Path.Combine(targetPath, fileName);
            FileInfo sourceFile = new FileInfo(filePath);
            FileInfo targetFile = new FileInfo(destFile);
            
            this.CurrentSourceFile = filePath;
            this.CurrentTargetFile = destFile;
            this.LastActionTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Utilisation du Polymorphisme
            if (strategy.ShouldCopy(sourceFile, targetFile))
            {
                Stopwatch sw = Stopwatch.StartNew();
                long transferTime;
                
                try 
                {
                    File.Copy(filePath, destFile, true);
                    sw.Stop();
                    transferTime = sw.ElapsedMilliseconds;
                }
                catch (Exception ex) 
                { 
                    sw.Stop();
                    transferTime = -1; // Temps négatif = erreur (requis par cahier des charges)
                    Console.WriteLine($"Error copying {fileName}: {ex.Message}");
                }
                
                // Log dans tous les cas (succès ou erreur)
                _logger.WriteLog(new EasyLog.LogEntry {
                    Timestamp = this.LastActionTimestamp,
                    BackupName = this.Name,
                    SourceFilePath = ToUNCPath(filePath), 
                    TargetFilePath = ToUNCPath(destFile),
                    FileSize = sourceFile.Length,
                    FileTransferTime = transferTime
                });
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
        try
        {
            // Si c'est déjà un chemin réseau, on ne touche à rien
            if (path.StartsWith(@"\\")) 
                return path;
            
            // Récupérer le chemin absolu
            string fullPath = Path.GetFullPath(path);
            
            // Convertir C:\ en \\localhost\C$\
            if (fullPath.Length >= 2 && fullPath[1] == ':')
            {
                return $@"\\localhost\{fullPath[0]}${fullPath.Substring(2)}";
            }
            
            return fullPath;
        }
        catch
        {
            return path; // En cas d'erreur, retourner le chemin original
        }
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