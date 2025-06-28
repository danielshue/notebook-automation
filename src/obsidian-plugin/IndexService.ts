/**
 * Recursively generates indexes for all subfolders of the given root folder.
 * @param rootFolder Absolute path to the root folder.
 * @param getResourceFiles Function to get resource files for a folder.
 * @param indexFileName Name of the index file to create (default: 'index.md').
 * @returns Array of absolute paths to all created index files.
 */
export function generateIndexesRecursively(
  rootFolder: string,
  getResourceFiles: (folder: string) => string[],
  indexFileName = 'index.md'
): string[] {
  const createdIndexes: string[] = [];
  function walk(folder: string) {
    if (!fs.existsSync(folder)) return;
    const resourceFiles = getResourceFiles(folder);
    if (resourceFiles.length > 0) {
      const indexPath = generateFolderIndex(folder, resourceFiles, indexFileName);
      createdIndexes.push(indexPath);
    }
    for (const entry of fs.readdirSync(folder, { withFileTypes: true })) {
      if (entry.isDirectory()) {
        walk(path.join(folder, entry.name));
      }
    }
  }
  walk(rootFolder);
  return createdIndexes;
}
import * as fs from 'fs';
import * as path from 'path';

/**
 * Generates an index (e.g., index.md) for the specified folder, listing resource files.
 * @param folderPath Absolute path to the folder to index.
 * @param resourceFiles Array of resource file names to include in the index.
 * @param indexFileName Name of the index file to create (default: 'index.md').
 * @returns Absolute path to the created index file.
 */
export function generateFolderIndex(folderPath: string, resourceFiles: string[], indexFileName = 'index.md'): string {
  if (!fs.existsSync(folderPath)) {
    throw new Error('Folder does not exist: ' + folderPath);
  }
  const lines = ['# Index', '', ...resourceFiles.map(f => `- [${f}](./${f})`)];
  const indexPath = path.join(folderPath, indexFileName);
  fs.writeFileSync(indexPath, lines.join('\n'), 'utf-8');
  return indexPath;
}
// Service for generating indexes for folders and subfolders

// ...index service implementation will go here...
