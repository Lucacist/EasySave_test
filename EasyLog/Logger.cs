using System.Text.Json; // Obligatoire pour le format JSON

namespace EasyLog;

public class Logger
{
    // On définit où on enregistre les logs (évite C:\temp\)
    private string _logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "Logs");

    public void WriteLog(LogEntry entry)
    {
        // 1. Créer le dossier s'il n'existe pas
        if (!Directory.Exists(_logFolderPath)) Directory.CreateDirectory(_logFolderPath);

        // 2. Nom du fichier : date du jour (ex: 2026-01-27.json)
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(_logFolderPath, fileName);

        List<LogEntry> logs;

        // 3. Si le fichier existe, on lit les anciens logs, sinon on crée une liste vide
        if (File.Exists(filePath))
        {
            string existingJson = File.ReadAllText(filePath);
            logs = JsonSerializer.Deserialize<List<LogEntry>>(existingJson) ?? new List<LogEntry>();
        }
        else
        {
            logs = new List<LogEntry>();
        }

        // 4. On ajoute la nouvelle ligne
        logs.Add(entry);

        // 5. On enregistre tout avec des retours à la ligne (WriteIndented)
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(logs, options);
        File.WriteAllText(filePath, jsonString);
    }
}