using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices;
using EasySaveApp.Services;
using EasySaveApp.Models;
using System.Text.Json;

namespace EasySaveGUI;

[ComVisible(true)]
public class Bridge
{
    // Dictionnaire pour garder les références aux jobs en cours d'exécution
    private static Dictionary<string, BackupJob> runningJobs = new Dictionary<string, BackupJob>();

    public string GetJobs()
    {
        var jobs = JobService.Instance.LoadState();
        return JsonSerializer.Serialize(jobs);
    }

    public void AddJob(string name, string source, string target, int type)
    {
        var job = new BackupJob(name, source, target, (BackupType)type);
        var jobs = JobService.Instance.LoadState();
        jobs.Add(job);
        JobService.Instance.SaveState(jobs);
    }

    public async Task ExecuteJob(string name)
    {
        var jobs = JobService.Instance.LoadState();
        var job = jobs.FirstOrDefault(j => j.Name == name);
        if (job != null)
        {
            // Stocker la référence du job en cours
            runningJobs[name] = job;
            
            await Task.Run(() => JobService.Instance.ExecuteJob(job, jobs));
            
            // Retirer la référence une fois terminé
            runningJobs.Remove(name);
        }
    }

    public void PauseJob(string name)
    {
        // Utiliser la référence du job en cours
        if (runningJobs.TryGetValue(name, out var job))
        {
            job.Pause();
        }
    }

    public void ResumeJob(string name)
    {
        // Utiliser la référence du job en cours
        if (runningJobs.TryGetValue(name, out var job))
        {
            job.Resume();
        }
    }

    public void CancelJob(string name)
    {
        // Utiliser la référence du job en cours
        if (runningJobs.TryGetValue(name, out var job))
        {
            job.Cancel();
            runningJobs.Remove(name);
        }
    }
}

public partial class Form1 : Form
{
    private WebView2 webView;

    public Form1()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        this.Controls.Add(webView);

        await webView.EnsureCoreWebView2Async(null);

        webView.CoreWebView2.AddHostObjectToScript("bridge", new Bridge());

        // Mapper le dossier wwwroot vers un domaine virtuel pour éviter CORS
        var wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "easysave.local", 
            wwwrootPath, 
            CoreWebView2HostResourceAccessKind.Allow
        );

        // Naviguer vers le domaine virtuel
        webView.CoreWebView2.Navigate("https://easysave.local/index.html");
    }
}
