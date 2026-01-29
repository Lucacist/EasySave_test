namespace EasySaveApp.Strategies;

public class FullBackupStrategy : IBackupStrategy
{
    // En sauvegarde complète, on copie toujours
    public bool ShouldCopy(FileInfo source, FileInfo target) => true;
}