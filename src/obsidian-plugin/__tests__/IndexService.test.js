"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const IndexService_1 = require("../IndexService");
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
        const getResourceFiles = (folder) => fs.readdirSync(folder).filter(f => f.endsWith('.pdf') || f.endsWith('.mp4'));
        const indexes = (0, IndexService_1.generateIndexesRecursively)(rootDir, getResourceFiles);
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
        const indexPath = (0, IndexService_1.generateFolderIndex)(testDir, ['file1.pdf', 'file2.mp4']);
        expect(fs.existsSync(indexPath)).toBe(true);
        const content = fs.readFileSync(indexPath, 'utf-8');
        expect(content).toContain('- [file1.pdf](./file1.pdf)');
        expect(content).toContain('- [file2.mp4](./file2.mp4)');
    });
    it('should throw if folder does not exist', () => {
        expect(() => (0, IndexService_1.generateFolderIndex)('/bad/folder', ['file.pdf'])).toThrow();
    });
});
// Unit tests for IndexService
describe('IndexService', () => {
    it('should ...', () => {
        // ...test implementation...
    });
});
