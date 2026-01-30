import { useState } from 'react';
import { bridge } from '@/types/bridge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Plus } from 'lucide-react';

interface AddJobFormProps {
  onJobAdded: () => void;
}

export function AddJobForm({ onJobAdded }: AddJobFormProps) {
  const [name, setName] = useState('');
  const [source, setSource] = useState('');
  const [target, setTarget] = useState('');
  const [type, setType] = useState(0);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name || !source || !target) {
      alert('Veuillez remplir tous les champs');
      return;
    }

    try {
      await bridge.AddJob(name, source, target, type);
      setName('');
      setSource('');
      setTarget('');
      setType(0);
      onJobAdded();
    } catch (error) {
      console.error('Erreur lors de l\'ajout du job:', error);
      alert('Erreur lors de l\'ajout du job');
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Plus className="w-5 h-5" />
          Nouvelle tâche de sauvegarde
        </CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="name" className="text-sm font-medium">
                Nom de la tâche
              </label>
              <Input
                id="name"
                placeholder="Ex: Sauvegarde Documents"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>
            
            <div className="space-y-2">
              <label htmlFor="type" className="text-sm font-medium">
                Type de sauvegarde
              </label>
              <select
                id="type"
                value={type}
                onChange={(e) => setType(Number(e.target.value))}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <option value={0}>Complète</option>
                <option value={1}>Différentielle</option>
              </select>
            </div>
          </div>

          <div className="space-y-2">
            <label htmlFor="source" className="text-sm font-medium">
              Répertoire source
            </label>
            <Input
              id="source"
              placeholder="C:\Users\Documents"
              value={source}
              onChange={(e) => setSource(e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="target" className="text-sm font-medium">
              Répertoire de destination
            </label>
            <Input
              id="target"
              placeholder="D:\Backups"
              value={target}
              onChange={(e) => setTarget(e.target.value)}
            />
          </div>

          <Button type="submit" className="w-full">
            <Plus className="w-4 h-4 mr-2" />
            Ajouter la tâche
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
