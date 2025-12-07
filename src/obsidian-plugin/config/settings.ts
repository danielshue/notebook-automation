export interface NotebookAutomationSettings {
  configPath: string;
  verbose?: boolean;
  debug?: boolean;
  dryRun?: boolean;
  force?: boolean;
  htmlExtensions?: string;
  pdfExtractImages?: boolean;
  bannersEnabled?: boolean;
  oneDriveSharedLink?: boolean;
  enableVideoSummary?: boolean;
  enablePdfSummary?: boolean;
  enableHtmlEpubTxtSummary?: boolean;
  enableIndexCreation?: boolean;
  enableEnsureMetadata?: boolean;
  enableDocumentPlaceholders?: boolean;
  unidirectionalSync?: boolean;
  recursiveDirectorySync?: boolean;
  recursiveTranscriptConsolidation?: boolean;
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
  htmlExtensions: ".html,.htm,.epub",
  pdfExtractImages: false,
  bannersEnabled: false,
  oneDriveSharedLink: true,
  enableVideoSummary: true,
  enablePdfSummary: true,
  enableHtmlEpubTxtSummary: true,
  enableIndexCreation: true,
  enableEnsureMetadata: true,
  enableDocumentPlaceholders: true,
  unidirectionalSync: true,
  recursiveDirectorySync: true,
  recursiveTranscriptConsolidation: false,
  recursiveIndexBuild: false,
  advancedConfiguration: false,
  baseBlockTemplateFilename: "BaseBlockTemplate.yml",
};
