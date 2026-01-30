export interface BackupJob {
  Name: string;
  SourceDirectory: string;
  TargetDirectory: string;
  Type: number;
  State: string;
  Progress: number;
  TotalFilesSize: number;
  TotalFilesCount: number;
  FilesItemsLeft: number;
  CurrentSourceFile: string;
}

declare global {
  interface Window {
    chrome: {
      webview: {
        hostObjects: {
          bridge: {
            GetJobs(): Promise<string>;
            AddJob(name: string, source: string, target: string, type: number): Promise<void>;
            ExecuteJob(name: string): Promise<void>;
            PauseJob(name: string): Promise<void>;
            ResumeJob(name: string): Promise<void>;
            CancelJob(name: string): Promise<void>;
          };
        };
      };
    };
  }
}

export const bridge = window.chrome?.webview?.hostObjects?.bridge;
