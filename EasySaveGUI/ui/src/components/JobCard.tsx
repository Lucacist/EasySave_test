import { BackupJob } from '@/types/bridge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';
import { Play, Pause, Square, RotateCw } from 'lucide-react';

interface JobCardProps {
  job: BackupJob;
  onExecute: (name: string) => void;
  onPause: (name: string) => void;
  onResume: (name: string) => void;
  onCancel: (name: string) => void;
}

export function JobCard({ job, onExecute, onPause, onResume, onCancel }: JobCardProps) {
  const getStateVariant = (state: string) => {
    switch (state.toLowerCase()) {
      case 'active': return 'default';
      case 'paused': return 'secondary';
      case 'completed': return 'outline';
      case 'cancelled': return 'destructive';
      case 'error': return 'destructive';
      default: return 'outline';
    }
  };

  const getTypeLabel = (type: number) => {
    return type === 0 ? 'Complète' : 'Différentielle';
  };

  const formatSize = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
  };

  const isPaused = job.State.toLowerCase() === 'paused';
  const isActive = job.State.toLowerCase() === 'active';
  const isCancelled = job.State.toLowerCase() === 'cancelled';
  const isIdle = job.State.toLowerCase() === 'idle';
  const isCompleted = job.State.toLowerCase() === 'completed';

  return (
    <Card className="hover:shadow-lg transition-shadow">
      <CardHeader>
        <div className="flex justify-between items-start">
          <div>
            <CardTitle className="text-xl">{job.Name}</CardTitle>
            <p className="text-sm text-muted-foreground mt-1">{getTypeLabel(job.Type)}</p>
          </div>
          <Badge variant={getStateVariant(job.State)}>
            {job.State}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <div className="flex justify-between text-sm">
            <span className="text-muted-foreground">Progression</span>
            <span className="font-medium">{job.Progress}%</span>
          </div>
          <Progress value={job.Progress} />
        </div>

        <div className="space-y-1 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Source:</span>
            <span className="truncate ml-2 max-w-[200px]" title={job.SourceDirectory}>
              {job.SourceDirectory}
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Destination:</span>
            <span className="truncate ml-2 max-w-[200px]" title={job.TargetDirectory}>
              {job.TargetDirectory}
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Fichiers:</span>
            <span>{job.TotalFilesCount - job.FilesItemsLeft} / {job.TotalFilesCount}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Taille:</span>
            <span>{formatSize(job.TotalFilesSize)}</span>
          </div>
        </div>

        {job.CurrentSourceFile && (
          <div className="text-xs text-muted-foreground truncate" title={job.CurrentSourceFile}>
            Fichier actuel: {job.CurrentSourceFile}
          </div>
        )}

        <div className="flex gap-2 pt-2">
          {(isIdle || isCancelled || isCompleted) && (
            <Button size="sm" onClick={() => onExecute(job.Name)} className="flex-1">
              <Play className="w-4 h-4 mr-1" />
              Lancer
            </Button>
          )}
          {isActive && (
            <Button size="sm" variant="secondary" onClick={() => onPause(job.Name)} className="flex-1">
              <Pause className="w-4 h-4 mr-1" />
              Pause
            </Button>
          )}
          {isPaused && (
            <Button size="sm" onClick={() => onResume(job.Name)} className="flex-1">
              <RotateCw className="w-4 h-4 mr-1" />
              Reprendre
            </Button>
          )}
          {(isActive || isPaused) && (
            <Button size="sm" variant="destructive" onClick={() => onCancel(job.Name)}>
              <Square className="w-4 h-4 mr-1" />
              Annuler
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
