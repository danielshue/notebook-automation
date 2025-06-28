import { importResourceFile, openResourceFile, mapVaultToOneDriveFolder, listResourceFiles } from '../ResourceService';
import * as fs from 'fs';
import * as path from 'path';

describe('importResourceFile', () => {
  const testSrcDir = path.join(__dirname, 'test-onedrive-folder-import');
  const testDestDir = path.join(__dirname, 'test-vault-folder-import');
  const testFile = path.join(testSrcDir, 'importme.pdf');
  beforeAll(() => {
    fs.mkdirSync(testSrcDir, { recursive: true });
    fs.mkdirSync(testDestDir, { recursive: true });
    fs.writeFileSync(testFile, 'testdata');
  });
  afterAll(() => {
    fs.rmSync(testSrcDir, { recursive: true, force: true });
    fs.rmSync(testDestDir, { recursive: true, force: true });
  });
  it('should copy a file from OneDrive to Vault', async () => {
    await importResourceFile(testFile, testDestDir);
    const destFile = path.join(testDestDir, 'importme.pdf');
    expect(fs.existsSync(destFile)).toBe(true);
    expect(fs.readFileSync(destFile, 'utf-8')).toBe('testdata');
  });
  it('should reject if source file does not exist', async () => {
    await expect(importResourceFile('/bad/path/file.pdf', testDestDir)).rejects.toThrow();
  });
});

describe('openResourceFile', () => {
  it('should reject on unsupported platforms', async () => {
    // Simulate an unknown platform
    const originalPlatform = process.platform;
    Object.defineProperty(process, 'platform', { value: 'unknown', configurable: true });
    await expect(openResourceFile('/path/to/file')).rejects.toThrow();
    Object.defineProperty(process, 'platform', { value: originalPlatform });
  });
  // Note: Actual open tests are not run to avoid launching files during test
});
// Unit tests for ResourceService


// ...existing code...
describe('listResourceFiles', () => {
  const testDir = path.join(__dirname, 'test-onedrive-folder');
  const emptyDir = path.join(__dirname, 'test-onedrive-empty');
  const noResourceDir = path.join(__dirname, 'test-onedrive-noresource');
  beforeAll(() => {
    fs.mkdirSync(testDir, { recursive: true });
    fs.writeFileSync(path.join(testDir, 'video.mp4'), '');
    fs.writeFileSync(path.join(testDir, 'doc.pdf'), '');
    fs.writeFileSync(path.join(testDir, 'sheet.xlsx'), '');
    fs.writeFileSync(path.join(testDir, 'page.html'), '');
    fs.writeFileSync(path.join(testDir, 'ignore.txt'), '');
    fs.mkdirSync(emptyDir, { recursive: true });
    fs.mkdirSync(noResourceDir, { recursive: true });
    fs.writeFileSync(path.join(noResourceDir, 'ignore.txt'), '');
  });
  afterAll(() => {
    fs.rmSync(testDir, { recursive: true, force: true });
    fs.rmSync(emptyDir, { recursive: true, force: true });
    fs.rmSync(noResourceDir, { recursive: true, force: true });
  });
  it('should list only supported resource files', () => {
    const files = listResourceFiles(testDir);
    expect(files.sort()).toEqual(['video.mp4', 'doc.pdf', 'sheet.xlsx', 'page.html'].sort());
  });
  it('should throw if folder does not exist', () => {
    expect(() => listResourceFiles('/non/existent/folder')).toThrow();
  });
  it('should throw if folder is empty', () => {
    expect(() => listResourceFiles(emptyDir)).toThrow('No files found in the OneDrive folder.');
  });
  it('should throw if no supported resource files are found', () => {
    expect(() => listResourceFiles(noResourceDir)).toThrow('No supported resource files found in the OneDrive folder.');
  });
});
import { PluginConfig } from '../config';

const config: PluginConfig = {
  vaultRoot: '/Users/test/Vault',
  oneDriveRoot: '/Users/test/OneDrive/Resources'
};

describe('mapVaultToOneDriveFolder', () => {
  it('should map a vault folder to the correct OneDrive folder', () => {
    const vaultFolder = '/Users/test/Vault/Notes/Math';
    const expected = '/Users/test/OneDrive/Resources/Notes/Math';
    expect(mapVaultToOneDriveFolder(vaultFolder, config)).toBe(expected);
  });

  it('should throw if the vault folder is outside the vault root', () => {
    const badFolder = '/Users/test/Other/Folder';
    expect(() => mapVaultToOneDriveFolder(badFolder, config)).toThrow();
  });
});
