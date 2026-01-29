namespace EasySaveApp.Strategies;

using EasySaveApp.Models;

public static class BackupStrategyFactory
{
    /// <summary>
    /// Crée la stratégie correspondante au type de sauvegarde.
    /// </summary>
    public static IBackupStrategy GetStrategy(BackupType type)
    {
        return type switch
        {
            BackupType.Full => new FullBackupStrategy(),
            BackupType.Differential => new DifferentialBackupStrategy(),
            _ => throw new ArgumentException("Type de sauvegarde non supporté")
        };
    }
}