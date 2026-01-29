# EasySave - Version 1.0

Application de sauvegarde en ligne de commande développée en .NET 8.0, conforme au cahier des charges du livrable 1.

## 📋 Table des matières

- [Vue d'ensemble](#vue-densemble)
- [Architecture du projet](#architecture-du-projet)
- [Design Patterns](#design-patterns)
- [Installation](#installation)
- [Utilisation](#utilisation)
- [Format des fichiers](#format-des-fichiers)
- [Conformité aux exigences](#conformité-aux-exigences)
- [Gestion des erreurs](#gestion-des-erreurs)
- [Limitations](#limitations)
- [Évolutions futures](#évolutions-futures)

---

## 🎯 Vue d'ensemble

EasySave est une solution de sauvegarde professionnelle permettant de créer et d'exécuter jusqu'à 5 travaux de sauvegarde (complète ou différentielle) vers des destinations locales, externes ou réseau. L'application génère des fichiers de logs journaliers et un fichier d'état temps réel au format JSON.

### Fonctionnalités principales

- ✅ Création de jusqu'à 5 travaux de sauvegarde
- ✅ Sauvegarde complète et différentielle
- ✅ Interface multilingue (Français/Anglais)
- ✅ Mode ligne de commande pour automatisation
- ✅ Logs journaliers au format JSON (format UNC)
- ✅ Fichier d'état temps réel
- ✅ Support disques locaux, externes et réseau

---

## 🏗️ Architecture du projet

L'application suit une architecture en **couches (N-Tier)**, facilitant la maintenance et la future migration vers MVVM (V2.0).

### Structure du projet

Le projet est divisé en **2 assemblies** :

#### 1. **EasyLog.dll** - Bibliothèque de journalisation réutilisable
```
EasyLog/
├── Logger.cs        # Gestion de l'écriture des logs journaliers
└── LogEntry.cs      # Modèle d'entrée de log
```

**Responsabilités :**
- Écriture des logs journaliers au format JSON
- Un fichier par jour (`YYYY-MM-DD.json`)
- Format UNC pour les chemins de fichiers
- Conçu pour être réutilisé dans d'autres projets

#### 2. **EasySaveApp.exe** - Application Console principale
```
EasySaveApp/
├── Program.cs                    # Point d'entrée et interface Console
├── Models/
│   ├── BackupJob.cs             # Modèle de travail de sauvegarde
│   └── BackupType.cs            # Enum (Full/Differential)
├── Services/
│   └── JobService.cs            # Singleton - Gestion des jobs et persistance
├── Strategies/
│   ├── IBackupStrategy.cs       # Interface Strategy
│   ├── FullBackupStrategy.cs    # Stratégie de sauvegarde complète
│   ├── DifferentialBackupStrategy.cs  # Stratégie différentielle
│   └── BackupStrategyFactory.cs # Factory pour créer les stratégies
└── Resources/
    ├── Messages.resx            # Ressources EN
    └── Messages.fr.resx         # Ressources FR
```

### Diagramme de classes (UML)

```mermaid
classDiagram
    class BackupJob {
        +string Name
        +string SourceDirectory
        +string TargetDirectory
        +BackupType Type
        +string State
        +long TotalFilesSize
        +long TotalFilesCount
        +long FilesItemsLeft
        +long FilesSizeLeft
        +int Progress
        +string CurrentSourceFile
        +string CurrentTargetFile
        +string LastActionTimestamp
        +event EventHandler OnProgress
        +Execute()
        -CopyAll()
        -ToUNCPath()
        -NotifyProgress()
        -CalculateStatistics()
    }

    class IBackupStrategy {
        <<interface>>
        +ShouldCopy(FileInfo source, FileInfo target) bool
    }

    class FullBackupStrategy {
        +ShouldCopy(FileInfo source, FileInfo target) bool
    }

    class DifferentialBackupStrategy {
        +ShouldCopy(FileInfo source, FileInfo target) bool
    }

    class BackupStrategyFactory {
        +GetStrategy(BackupType type) IBackupStrategy$
    }

    class JobService {
        <<Singleton>>
        -static JobService _instance
        +Instance JobService$
        +CreateJob() BackupJob
        +DisplayJobs(List jobs)
        +ExecuteJob(BackupJob job, List allJobs)
        +SaveState(List jobs)
        +LoadState() List
    }

    class Logger {
        +WriteLog(LogEntry entry)
    }

    class LogEntry {
        +string Timestamp
        +string BackupName
        +string SourceFilePath
        +string TargetFilePath
        +long FileSize
        +long FileTransferTime
    }

    BackupJob --> IBackupStrategy : utilise
    IBackupStrategy <|-- FullBackupStrategy
    IBackupStrategy <|-- DifferentialBackupStrategy
    BackupStrategyFactory ..> IBackupStrategy : crée
    JobService o-- BackupJob : gère
    BackupJob --> Logger : écrit logs
    Logger --> LogEntry : écrit
```

---

## 🎨 Design Patterns

### 1. Singleton (Patron de Création)

**Classe :** `JobService`

**Justification :** Garantir qu'il n'existe qu'une seule instance du service gérant les fichiers (`state.json`). Cela évite les conflits d'accès concurrents et centralise la logique de chargement/sauvegarde.

```csharp
public static JobService Instance => _instance ??= new JobService();
```

### 2. Strategy (Patron de Comportement)

**Classes :** `IBackupStrategy`, `FullBackupStrategy`, `DifferentialBackupStrategy`

**Justification :** Isoler l'algorithme de décision de copie. Le `BackupJob` ne sait pas *comment* décider s'il doit copier un fichier ; il délègue cette tâche à une stratégie. Cela permet d'ajouter de nouveaux types de backup (ex: incrémental, compressé) sans modifier le code existant.

**Principe SOLID respecté :** Open/Closed Principle

```csharp
// Sauvegarde complète : copie tout
public bool ShouldCopy(FileInfo source, FileInfo target) => true;

// Sauvegarde différentielle : copie si modifié
public bool ShouldCopy(FileInfo source, FileInfo target) 
    => !target.Exists || source.LastWriteTime > target.LastWriteTime;
```

### 3. Factory (Patron de Création)

**Classe :** `BackupStrategyFactory`

**Justification :** Simplifier la création des stratégies. On passe un `BackupType` (Enum) et la fabrique retourne l'objet approprié.

```csharp
public static IBackupStrategy GetStrategy(BackupType type)
{
    return type switch
    {
        BackupType.Full => new FullBackupStrategy(),
        BackupType.Differential => new DifferentialBackupStrategy(),
        _ => throw new ArgumentException("Unknown backup type")
    };
}
```

### 4. Observer (Patron de Comportement)

**Implémentation :** Événement `OnProgress` dans `BackupJob`

**Justification :** Découpler totalement le moteur de sauvegarde du système de sauvegarde d'état. Le job "notifie" ses progrès, et le service (l'observateur) réagit en écrivant dans le JSON. C'est la base de la communication temps réel demandée.

```csharp
// BackupJob déclenche l'événement
OnProgress?.Invoke(this, new ProgressEventArgs { Job = this });

// JobService s'abonne
job.OnProgress += (sender, e) => { this.SaveState(allJobs); };
```

---

## 🚀 Installation

### Prérequis

- .NET 8.0 SDK ou Runtime
- Windows (testé sur Windows 10/11)

### Compilation depuis les sources

```powershell
# Cloner le projet
git clone <url-du-repo>
cd EasySave

# Compiler la solution
dotnet build EasySave.slnx

# L'exécutable se trouve dans :
# EasySaveApp\bin\Debug\net8.0\EasySaveApp.exe
```

### Fichiers générés

Les fichiers de données sont stockés dans :
```
%APPDATA%\EasySave\
├── state.json           # État temps réel
└── Logs/
    ├── 2026-01-29.json # Logs journaliers
    ├── 2026-01-30.json
    └── ...
```

---

## 💻 Utilisation

### Mode Interactif

```powershell
cd EasySaveApp\bin\Debug\net8.0
.\EasySaveApp.exe
```

**Menu principal :**
```
Choose Language / Choisissez la langue (en/fr): fr

--- Menu EasySave ---
1. Créer un travail de sauvegarde
2. Lister les travaux
3. Exécuter les sauvegardes
4. Quitter
```

### Mode Ligne de Commande (CLI)

Pour automatiser les sauvegardes (scripts, tâches planifiées) :

#### Exécuter une plage de jobs
```powershell
.\EasySaveApp.exe 1-3
# Exécute les jobs 1, 2 et 3 séquentiellement
```

#### Exécuter des jobs spécifiques
```powershell
.\EasySaveApp.exe 1;3;5
# Exécute les jobs 1, 3 et 5
```

#### Exécuter un seul job
```powershell
.\EasySaveApp.exe 2
# Exécute uniquement le job 2
```

**Note :** En mode CLI, la langue par défaut est détectée automatiquement. Utilisez un pipe pour forcer :
```powershell
echo "fr" | .\EasySaveApp.exe 1-3
```

---

## 📄 Format des fichiers

### state.json (Fichier d'état temps réel)

Mis à jour en temps réel pendant l'exécution des sauvegardes.

```json
[
  {
    "Name": "BackupWeb",
    "SourceDirectory": "C:\\Sites\\Web",
    "TargetDirectory": "D:\\Backup\\Web",
    "Type": 0,
    "State": "Active",
    "TotalFilesSize": 104857600,
    "TotalFilesCount": 350,
    "FilesItemsLeft": 120,
    "FilesSizeLeft": 35651584,
    "Progress": 65,
    "CurrentSourceFile": "C:\\Sites\\Web\\images\\photo.jpg",
    "CurrentTargetFile": "D:\\Backup\\Web\\images\\photo.jpg",
    "LastActionTimestamp": "29/01/2026 19:30:45"
  },
  {
    "Name": "BackupDocs",
    "SourceDirectory": "C:\\Documents",
    "TargetDirectory": "\\\\NAS\\Backup\\Docs",
    "Type": 1,
    "State": "Idle",
    "TotalFilesSize": 52428800,
    "TotalFilesCount": 180,
    "FilesItemsLeft": 0,
    "FilesSizeLeft": 0,
    "Progress": 100,
    "CurrentSourceFile": "",
    "CurrentTargetFile": "",
    "LastActionTimestamp": "29/01/2026 18:45:12"
  }
]
```

**États possibles :**
- `Idle` : En attente
- `Active` : Sauvegarde en cours

**Types de sauvegarde :**
- `0` : Complète (Full)
- `1` : Différentielle (Differential)

### Log journalier (YYYY-MM-DD.json)

Un fichier par jour contenant toutes les actions de sauvegarde.

```json
[
  {
    "Timestamp": "29/01/2026 19:30:42",
    "BackupName": "BackupWeb",
    "SourceFilePath": "\\\\localhost\\C$\\Sites\\Web\\index.html",
    "TargetFilePath": "\\\\localhost\\D$\\Backup\\Web\\index.html",
    "FileSize": 4096,
    "FileTransferTime": 15
  },
  {
    "Timestamp": "29/01/2026 19:30:43",
    "BackupName": "BackupWeb",
    "SourceFilePath": "\\\\localhost\\C$\\Sites\\Web\\style.css",
    "TargetFilePath": "\\\\localhost\\D$\\Backup\\Web\\style.css",
    "FileSize": 2048,
    "FileTransferTime": 8
  },
  {
    "Timestamp": "29/01/2026 19:31:10",
    "BackupName": "BackupWeb",
    "SourceFilePath": "\\\\localhost\\C$\\Sites\\Web\\locked.db",
    "TargetFilePath": "\\\\localhost\\D$\\Backup\\Web\\locked.db",
    "FileSize": 8192,
    "FileTransferTime": -1
  }
]
```

**Champs :**
- `FileTransferTime` : Temps en millisecondes. **Négatif si erreur** (fichier verrouillé, accès refusé, etc.)
- Chemins au **format UNC** (`\\localhost\C$\...`) pour compatibilité serveur

---

## 📊 Diagramme de séquence : Exécution d'un Job

```mermaid
sequenceDiagram
    participant UI as Program.cs
    participant JS as JobService
    participant BJ as BackupJob
    participant Factory as StrategyFactory
    participant Strat as IBackupStrategy
    participant Log as EasyLog.Logger

    UI->>JS: ExecuteJob(job, allJobs)
    JS->>BJ: S'abonne à OnProgress
    JS->>BJ: Execute()
    BJ->>BJ: CalculateStatistics()
    BJ->>Factory: GetStrategy(Type)
    Factory-->>BJ: StrategyInstance
    BJ->>BJ: State = "Active"
    
    loop Pour chaque fichier
        BJ->>Strat: ShouldCopy(source, target)?
        Strat-->>BJ: true/false
        
        alt Fichier à copier
            BJ->>BJ: File.Copy()
            BJ->>Log: WriteLog(LogEntry)
            Log->>Log: Écriture dans YYYY-MM-DD.json
        end
        
        BJ->>BJ: Mise à jour compteurs (Progress, FilesLeft...)
        BJ->>JS: NotifyProgress() [Event]
        JS->>JS: SaveState(allJobs) → state.json
    end
    
    BJ->>BJ: State = "Idle", Progress = 100
    BJ->>JS: NotifyProgress() [Event final]
    JS->>JS: SaveState(allJobs)
    BJ-->>UI: Fin de tâche
```

---

## ✅ Conformité aux exigences

| Exigence | Implémentation | Statut |
|----------|----------------|:------:|
| Application Console .NET | .NET 8.0 Console App | ✅ |
| Max 5 travaux de sauvegarde | Vérification dans `Program.cs` (case "1") | ✅ |
| Définition d'un travail | Modèle `BackupJob` avec toutes les propriétés | ✅ |
| Types de sauvegarde (Complète/Différentielle) | Pattern Strategy | ✅ |
| Support Anglais/Français | Fichiers `.resx` + `CultureInfo` | ✅ |
| Exécution d'un ou tous les travaux | Mode interactif (case "3") | ✅ |
| Exécution par ligne de commande | Parsing des `args[]` avec formats `1-3` et `1;3` | ✅ |
| Disques locaux/externes/réseau | Chemins absolus et UNC supportés | ✅ |
| Sauvegarde complète (fichiers + sous-répertoires) | `CopyAll()` récursif | ✅ |
| Log journalier temps réel | `EasyLog.dll` avec horodatage | ✅ |
| Informations minimales dans le log | Timestamp, nom, chemins UNC, taille, temps transfert | ✅ |
| DLL séparée EasyLog | Projet indépendant | ✅ |
| Fichier état temps réel | `state.json` avec Pattern Observer | ✅ |
| Informations état (nom, timestamp, état, progression) | Toutes les propriétés requises | ✅ |
| Emplacement fichiers compatible serveur | `%APPDATA%\EasySave\` | ✅ |
| Format JSON avec retours à la ligne | `WriteIndented = true` | ✅ |

---

## 🌍 Internationalisation (i18n)

### Implémentation

**Pattern :** Satellite Assembly (méthode standard .NET)

**Fichiers de ressources :**
- `Messages.resx` → Anglais (langue par défaut)
- `Messages.fr.resx` → Français

**Utilisation dans le code :**
```csharp
Console.WriteLine(EasySaveApp.Resources.Messages.MenuTitle);
```

**Sélection de la langue :**
```csharp
var culture = new CultureInfo(langChoice == "fr" ? "fr-FR" : "en-US");
CultureInfo.CurrentUICulture = culture;
CultureInfo.CurrentCulture = culture;
```

---

## ⚠️ Gestion des erreurs

### Stratégies de robustesse

| Scénario | Gestion | Comportement |
|----------|---------|--------------|
| Fichier verrouillé | `try-catch` + log | Temps de transfert = -1 dans le log |
| Répertoire cible inexistant | `Directory.CreateDirectory()` | Création automatique |
| Désérialisation JSON échouée | `catch` + liste vide | Retourne `new List<BackupJob>()` |
| Console redirigée (pipe) | `Console.IsInputRedirected` | Skip `ReadKey()` |
| Répertoire source inexistant | Vérification `Directory.Exists()` | Pas d'exécution |
| Job index invalide (CLI) | Vérification range | Message d'erreur explicite |

### Exemple de log d'erreur
```json
{
  "Timestamp": "29/01/2026 19:31:10",
  "BackupName": "BackupDB",
  "SourceFilePath": "\\\\localhost\\C$\\Database\\data.mdf",
  "TargetFilePath": "\\\\localhost\\D$\\Backup\\data.mdf",
  "FileSize": 10485760,
  "FileTransferTime": -1  // ⚠️ Erreur : fichier verrouillé par SQL Server
}
```

---

## 🚧 Limitations de la V1.0

- **Maximum 5 travaux** de sauvegarde configurables
- **Exécution séquentielle** uniquement (pas de parallélisme)
- **Pas de reprise** après échec (un fichier échoué ne stoppe pas la sauvegarde)
- **Pas de compression** des fichiers
- **Pas d'exclusion** de fichiers/dossiers (pattern)
- **Pas de notification** en fin de sauvegarde
- **Interface Console** uniquement (pas d'UI graphique)
- **Logs sans rotation** automatique (un fichier par jour sans suppression automatique)

---

## 🔮 Évolutions futures (V2.0)

### Transition vers MVVM

L'architecture actuelle en couches facilite la migration vers **MVVM (Model-View-ViewModel)** :

```
Couche actuelle          →  MVVM V2.0
─────────────────────────────────────────
Program.cs (Console)     →  View (WPF/Avalonia)
JobService               →  ViewModel
BackupJob                →  Model (inchangé)
IBackupStrategy          →  Model (inchangé)
```

### Fonctionnalités envisagées

- 🖥️ Interface graphique (WPF + MVVM)
- ⏸️ Pause/Reprise des sauvegardes
- 🔄 Sauvegarde incrémentale
- 📦 Compression/Chiffrement
- 📧 Notifications (email, toast)
- 🎯 Exclusion de fichiers (patterns/regex)
- 📊 Dashboard de statistiques
- ⏰ Planification automatique (scheduler intégré)
- 🌐 Support MacOS/Linux (.NET Multi-platform)

---

## 📞 Support

Pour toute question ou problème :
- 📖 Consultez ce README
- 🐛 Vérifiez les logs dans `%APPDATA%\EasySave\Logs\`
- 📋 Vérifiez le `state.json` pour l'état des jobs

---

## 📜 Licence

Projet académique - CESI École d'Ingénieurs
© 2026 - Tous droits réservés

---

**Version :** 1.0  
**Date :** Janvier 2026  
**Framework :** .NET 8.0  
**Langage :** C# 12
