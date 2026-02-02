using System.Text.Json;

namespace EasyLog;

public class Logger
{
    private readonly string _logFolderPath;

    // Constructeur par défaut
    public Logger()
    {
        _logFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "EasySave", 
            "Logs"
        );
    }

    // Constructeur avec chemin personnalisé (utile pour les tests ou serveurs)
    public Logger(string customPath)
    {
        _logFolderPath = customPath;
    }

    public void WriteLog(LogEntry entry)
    {
        // 1. Créer le dossier s'il n'existe pas
        if (!Directory.Exists(_logFolderPath)) 
            Directory.CreateDirectory(_logFolderPath);

        // 2. Nom du fichier : date du jour (ex: 2026-01-27.json)
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(_logFolderPath, fileName);

        List<LogEntry> logs;

        // 3. Si le fichier existe, on lit les anciens logs
        if (File.Exists(filePath))
        {
            string existingJson = File.ReadAllText(filePath);
            logs = JsonSerializer.Deserialize<List<LogEntry>>(existingJson) ?? new List<LogEntry>();
        }
        else
        {
            logs = new List<LogEntry>();
        }

        // 4. On ajoute la nouvelle entrée
        logs.Add(entry);

        // 5. On enregistre avec retours à la ligne
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(logs, options);
        File.WriteAllText(filePath, jsonString);
    }

    // Getter pour récupérer le chemin (utile pour debug/affichage)
    public string GetLogFolderPath() => _logFolderPath;
}