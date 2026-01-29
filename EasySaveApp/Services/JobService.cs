using EasySaveApp.Models;
using System.Text.Json;

namespace EasySaveApp.Services;

public class JobService
{
    // On ajoute un '?' pour dire qu'au tout début, c'est null (supprime le warning)
    private static JobService? _instance;
    
    public static JobService Instance => _instance ??= new JobService();
    
    private JobService() { }
    
    public BackupJob CreateJob()
    {
        Console.WriteLine(Resources.Messages.NewJobHeader);

        Console.Write(Resources.Messages.PromptName);
        string name = Console.ReadLine() ?? "";
        
        Console.Write(Resources.Messages.PromptSource);
        string source = Console.ReadLine() ?? "";
        
        Console.Write(Resources.Messages.PromptTarget);
        string target = Console.ReadLine() ?? "";

        Console.WriteLine(Resources.Messages.PromptType);
        string choice = Console.ReadLine() ?? "";
    
        BackupType type = (choice == "1") ? BackupType.Full : BackupType.Differential;

        return new BackupJob(name, source, target, type);
    }
    
    public void DisplayJobs(List<BackupJob> jobs)
    {
        Console.WriteLine($"\n--- {Resources.Messages.CurrentJobsTitle} ---");
    
        if (jobs.Count == 0)
        {
            Console.WriteLine("No jobs configured.");
            return;
        }

        for (int i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];
            Console.WriteLine($"{i + 1}. [Name: {job.Name}] [Type: {job.Type}]");
            Console.WriteLine($"   Source: {job.SourceDirectory}");
            Console.WriteLine($"   Target: {job.TargetDirectory}");
        }
    }
    
    public void ExecuteJob(BackupJob job, List<BackupJob> allJobs)
    {
        // On s'abonne à l'événement de progression
        job.OnProgress += (sender, e) =>
        {
            this.SaveState(allJobs);
        };

        job.Execute();
    }

    // Méthode interne pour centraliser le chemin du fichier (évite les erreurs de frappe)
    private string GetStateFilePath()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        return Path.Combine(folder, "state.json");
    }

    public void SaveState(List<BackupJob> jobs)
    {
        string path = GetStateFilePath();
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(jobs, options);

        File.WriteAllText(path, jsonString);
    }
    
    public List<BackupJob> LoadState()
    {
        string path = GetStateFilePath();
    
        if (!File.Exists(path)) return new List<BackupJob>();

        try {
            string jsonString = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<BackupJob>>(jsonString) ?? new List<BackupJob>();
        }
        catch {
            return new List<BackupJob>();
        }
    }
}