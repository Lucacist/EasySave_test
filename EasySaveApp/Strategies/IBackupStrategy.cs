namespace EasySaveApp.Strategies;

public interface IBackupStrategy
{
    // Retourne vrai si le fichier doit être copié
    bool ShouldCopy(FileInfo source, FileInfo target);
}