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
exports.generateIndexesRecursively = generateIndexesRecursively;
exports.generateFolderIndex = generateFolderIndex;
/**
 * Recursively generates indexes for all subfolders of the given root folder.
 * @param rootFolder Absolute path to the root folder.
 * @param getResourceFiles Function to get resource files for a folder.
 * @param indexFileName Name of the index file to create (default: 'index.md').
 * @returns Array of absolute paths to all created index files.
 */
function generateIndexesRecursively(rootFolder, getResourceFiles, indexFileName = 'index.md') {
    const createdIndexes = [];
    function walk(folder) {
        if (!fs.existsSync(folder))
            return;
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
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
/**
 * Generates an index (e.g., index.md) for the specified folder, listing resource files.
 * @param folderPath Absolute path to the folder to index.
 * @param resourceFiles Array of resource file names to include in the index.
 * @param indexFileName Name of the index file to create (default: 'index.md').
 * @returns Absolute path to the created index file.
 */
function generateFolderIndex(folderPath, resourceFiles, indexFileName = 'index.md') {
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
