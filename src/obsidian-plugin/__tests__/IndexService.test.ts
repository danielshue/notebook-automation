import * as fs from 'fs';
import * as path from 'path';
import { generateIndexesRecursively, generateFolderIndex } from '../IndexService';

describe('generateIndexesRecursively', () => {
  const rootDir = path.join(__dirname, 'test-index-recursive');
  const subDir = path.join(rootDir, 'sub');
  beforeAll(() => {
    fs.mkdirSync(rootDir, { recursive: true });
    fs.mkdirSync(subDir, { recursive: true });
    fs.writeFileSync(path.join(rootDir, 'file1.pdf'), '');
    fs.writeFileSync(path.join(subDir, 'file2.mp4'), '');
  });
  afterAll(() => {
    fs.rmSync(rootDir, { recursive: true, force: true });
  });
  it('should create indexes in all folders with resource files', () => {
    const getResourceFiles = (folder: string) =>
      fs.readdirSync(folder).filter(f => f.endsWith('.pdf') || f.endsWith('.mp4'));
    const indexes = generateIndexesRecursively(rootDir, getResourceFiles);
    expect(indexes.length).toBe(2);
    expect(fs.existsSync(indexes[0])).toBe(true);
    expect(fs.existsSync(indexes[1])).toBe(true);
  });
});

describe('generateFolderIndex', () => {
  const testDir = path.join(__dirname, 'test-index-folder');
  beforeAll(() => {
    fs.mkdirSync(testDir, { recursive: true });
    fs.writeFileSync(path.join(testDir, 'file1.pdf'), '');
    fs.writeFileSync(path.join(testDir, 'file2.mp4'), '');
  });
  afterAll(() => {
    fs.rmSync(testDir, { recursive: true, force: true });
  });
  it('should create an index file listing the resource files', () => {
    const indexPath = generateFolderIndex(testDir, ['file1.pdf', 'file2.mp4']);
    expect(fs.existsSync(indexPath)).toBe(true);
    const content = fs.readFileSync(indexPath, 'utf-8');
    expect(content).toContain('- [file1.pdf](./file1.pdf)');
    expect(content).toContain('- [file2.mp4](./file2.mp4)');
  });
  it('should throw if folder does not exist', () => {
    expect(() => generateFolderIndex('/bad/folder', ['file.pdf'])).toThrow();
  });
});
// Unit tests for IndexService

describe('IndexService', () => {
  it('should ...', () => {
    // ...test implementation...
  });
});
