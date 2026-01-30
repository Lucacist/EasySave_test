import { useEffect, useState } from 'react';
import { BackupJob, bridge } from '@/types/bridge';
import { JobCard } from '@/components/JobCard';
import { AddJobForm } from '@/components/AddJobForm';
import { HardDrive } from 'lucide-react';

function App() {
  const [jobs, setJobs] = useState<BackupJob[]>([]);

  const loadJobs = async () => {
    try {
      const jobsJson = await bridge.GetJobs();
      const parsedJobs = JSON.parse(jobsJson);
      setJobs(parsedJobs);
    } catch (error) {
      console.error('Erreur lors du chargement des jobs:', error);
    }
  };

  useEffect(() => {
    loadJobs();
    const interval = setInterval(loadJobs, 500);
    return () => clearInterval(interval);
  }, []);

  const handleExecute = async (name: string) => {
    try {
      await bridge.ExecuteJob(name);
    } catch (error) {
      console.error('Erreur lors de l\'exécution:', error);
      alert('Erreur lors de l\'exécution du job');
    }
  };

  const handlePause = async (name: string) => {
    try {
      await bridge.PauseJob(name);
    } catch (error) {
      console.error('Erreur lors de la pause:', error);
    }
  };

  const handleResume = async (name: string) => {
    try {
      await bridge.ResumeJob(name);
    } catch (error) {
      console.error('Erreur lors de la reprise:', error);
    }
  };

  const handleCancel = async (name: string) => {
    try {
      await bridge.CancelJob(name);
    } catch (error) {
      console.error('Erreur lors de l\'annulation:', error);
    }
  };

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        <header className="text-center space-y-2">
          <div className="flex items-center justify-center gap-3">
            <HardDrive className="w-10 h-10 text-primary" />
            <h1 className="text-4xl font-bold bg-gradient-to-r from-purple-600 to-blue-600 bg-clip-text text-transparent">
              EasySave
            </h1>
          </div>
          <p className="text-muted-foreground">
            Gestionnaire de sauvegardes v2.0
          </p>
        </header>

        <AddJobForm onJobAdded={loadJobs} />

        <div>
          <h2 className="text-2xl font-semibold mb-4 flex items-center gap-2">
            <HardDrive className="w-6 h-6" />
            Tâches de sauvegarde
            <span className="text-lg font-normal text-muted-foreground">
              ({jobs.length})
            </span>
          </h2>
          
          {jobs.length === 0 ? (
            <div className="text-center py-12 text-muted-foreground">
              Aucune tâche de sauvegarde. Créez-en une ci-dessus.
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {jobs.map((job) => (
                <JobCard
                  key={job.Name}
                  job={job}
                  onExecute={handleExecute}
                  onPause={handlePause}
                  onResume={handleResume}
                  onCancel={handleCancel}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
