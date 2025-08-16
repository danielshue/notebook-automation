export interface NotebookAutomationSettings {
  configPath: string;
  verbose?: boolean;
  debug?: boolean;
  dryRun?: boolean;
  force?: boolean;
  pdfExtractImages?: boolean;
  bannersEnabled?: boolean;
  oneDriveSharedLink?: boolean;
  enableVideoSummary?: boolean;
  enablePdfSummary?: boolean;
  enableIndexCreation?: boolean;
  enableEnsureMetadata?: boolean;
  enableDocumentPlaceholders?: boolean;
  unidirectionalSync?: boolean;
  recursiveDirectorySync?: boolean;
  recursiveIndexBuild?: boolean;
  advancedConfiguration?: boolean;
  baseBlockTemplateFilename?: string;
}

export const DEFAULT_SETTINGS: NotebookAutomationSettings = {
  configPath: "",
  verbose: false,
  debug: false,
  dryRun: false,
  force: false,
  pdfExtractImages: false,
  bannersEnabled: false,
  oneDriveSharedLink: true,
  enableVideoSummary: true,
  enablePdfSummary: true,
  enableIndexCreation: true,
  enableEnsureMetadata: true,
  enableDocumentPlaceholders: true,
  unidirectionalSync: true,
  recursiveDirectorySync: true,
  recursiveIndexBuild: false,
  advancedConfiguration: false,
  baseBlockTemplateFilename: "BaseBlockTemplate.yml",
};
