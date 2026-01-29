namespace EasySaveApp.Strategies;

public class DifferentialBackupStrategy : IBackupStrategy
{
    public bool ShouldCopy(FileInfo source, FileInfo target)
    {
        // On copie si la destination n'existe pas ou si la source est plus récente
        return !target.Exists || source.LastWriteTime > target.LastWriteTime;
    }
}