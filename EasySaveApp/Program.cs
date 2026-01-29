using EasySaveApp.Models;
using EasySaveApp.Services;
using System.Diagnostics;
using System.Globalization; // Nécessaire pour la culture

// Choix de la langue au démarrage
Console.WriteLine("Choose Language / Choisissez la langue (en/fr): ");
string langChoice = Console.ReadLine()?.ToLower() ?? "en";
var culture = new CultureInfo(langChoice == "fr" ? "fr-FR" : "en-US");

// On applique la culture à TOUTE l'application
CultureInfo.CurrentUICulture = culture;
CultureInfo.CurrentCulture = culture;

bool keepRunning = true;
JobService jobService = JobService.Instance;
List<BackupJob> jobs = jobService.LoadState();



// --- GESTION DES ARGUMENTS (LIGNE DE COMMANDE) ---
if (args.Length > 0)
{
    Console.WriteLine($"Command line mode detected... {jobs.Count} jobs found in state.json");
    
    string fullArgs = string.Join("", args);
    List<int> jobsToRun = new List<int>();

    // Nettoyage de la chaîne (enlève les espaces éventuels)
    fullArgs = fullArgs.Replace(" ", "");

    if (fullArgs.Contains("-"))
    {
        var range = fullArgs.Split('-');
        if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
        {
            for (int i = start; i <= end; i++) jobsToRun.Add(i);
        }
    }
    else if (fullArgs.Contains(";"))
    {
        var indices = fullArgs.Split(';');
        foreach (var s in indices)
        {
            if (int.TryParse(s, out int idx)) jobsToRun.Add(idx);
        }
    }
    else if (int.TryParse(fullArgs, out int idx))
    {
        jobsToRun.Add(idx);
    }

    // Exécution
    foreach (int index in jobsToRun)
    {
        // On vérifie si l'index existe dans notre liste chargée
        if (index > 0 && index <= jobs.Count)
        {
            var job = jobs[index - 1];
            Console.WriteLine($"> [{DateTime.Now:HH:mm:ss}] Starting Job {index}: {job.Name}...");
            
            // IMPORTANT : Utiliser le service pour activer l'Observer !
            jobService.ExecuteJob(job, jobs);
            
            Console.WriteLine($"> [{DateTime.Now:HH:mm:ss}] Job {index} finished.");
        }
        else
        {
            Console.WriteLine($"> Error: Job index {index} is out of range (1-{jobs.Count}).");
        }
    }

    Console.WriteLine("\nAll tasks completed.");
    // On ne fait ReadKey que si la console n'est pas redirigée
    if (!Console.IsInputRedirected)
    {
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
    return; 
}

while (keepRunning)
{
    Console.WriteLine(EasySaveApp.Resources.Messages.MenuTitle);
    Console.WriteLine(EasySaveApp.Resources.Messages.MenuCreate);
    Console.WriteLine(EasySaveApp.Resources.Messages.MenuList);
    Console.WriteLine(EasySaveApp.Resources.Messages.MenuExecute);
    Console.WriteLine(EasySaveApp.Resources.Messages.MenuExit);
    Console.Write(EasySaveApp.Resources.Messages.SelectOption);

    string choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            if (jobs.Count < 5) // Limite V1.0
            {
                jobs.Add(jobService.CreateJob());
                jobService.SaveState(jobs); // On utilise SaveState pour enregistrer la config
            }
            else
            {
                Console.WriteLine("Error: Max 5 jobs.");
            }
            break;

        case "2":
            jobService.DisplayJobs(jobs);
            // ASTUCE : Ouvre le dossier du state.json automatiquement
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            Process.Start("explorer.exe", path); 
            break;

        case "3":
            jobService.DisplayJobs(jobs);
            if (jobs.Count > 0)
            {
                if (int.TryParse(Console.ReadLine(), out int jobIndex) && jobIndex > 0 && jobIndex <= jobs.Count)
                {
                    // Utilisation de la méthode qui gère l'abonnement Observer
                    jobService.ExecuteJob(jobs[jobIndex - 1], jobs);
                }
            }
            break;

        case "4":
            keepRunning = false;
            break;

        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}