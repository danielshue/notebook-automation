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
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
const ResourceService_1 = require("../ResourceService");
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
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
    it('should copy a file from OneDrive to Vault', () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, ResourceService_1.importResourceFile)(testFile, testDestDir);
        const destFile = path.join(testDestDir, 'importme.pdf');
        expect(fs.existsSync(destFile)).toBe(true);
        expect(fs.readFileSync(destFile, 'utf-8')).toBe('testdata');
    }));
    it('should reject if source file does not exist', () => __awaiter(void 0, void 0, void 0, function* () {
        yield expect((0, ResourceService_1.importResourceFile)('/bad/path/file.pdf', testDestDir)).rejects.toThrow();
    }));
});
describe('openResourceFile', () => {
    it('should reject on unsupported platforms', () => __awaiter(void 0, void 0, void 0, function* () {
        // Simulate an unknown platform
        const originalPlatform = process.platform;
        Object.defineProperty(process, 'platform', { value: 'unknown', configurable: true });
        yield expect((0, ResourceService_1.openResourceFile)('/path/to/file')).rejects.toThrow();
        Object.defineProperty(process, 'platform', { value: originalPlatform });
    }));
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
        const files = (0, ResourceService_1.listResourceFiles)(testDir);
        expect(files.sort()).toEqual(['video.mp4', 'doc.pdf', 'sheet.xlsx', 'page.html'].sort());
    });
    it('should throw if folder does not exist', () => {
        expect(() => (0, ResourceService_1.listResourceFiles)('/non/existent/folder')).toThrow();
    });
    it('should throw if folder is empty', () => {
        expect(() => (0, ResourceService_1.listResourceFiles)(emptyDir)).toThrow('No files found in the OneDrive folder.');
    });
    it('should throw if no supported resource files are found', () => {
        expect(() => (0, ResourceService_1.listResourceFiles)(noResourceDir)).toThrow('No supported resource files found in the OneDrive folder.');
    });
});
const config = {
    vaultRoot: '/Users/test/Vault',
    oneDriveRoot: '/Users/test/OneDrive/Resources'
};
describe('mapVaultToOneDriveFolder', () => {
    it('should map a vault folder to the correct OneDrive folder', () => {
        const vaultFolder = '/Users/test/Vault/Notes/Math';
        const expected = '/Users/test/OneDrive/Resources/Notes/Math';
        expect((0, ResourceService_1.mapVaultToOneDriveFolder)(vaultFolder, config)).toBe(expected);
    });
    it('should throw if the vault folder is outside the vault root', () => {
        const badFolder = '/Users/test/Other/Folder';
        expect(() => (0, ResourceService_1.mapVaultToOneDriveFolder)(badFolder, config)).toThrow();
    });
});
