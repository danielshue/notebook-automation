#!/usr/bin/env node

import { readFileSync, writeFileSync, copyFileSync, existsSync, mkdirSync, readdirSync } from "fs";
import { resolve, join } from "path";

/**
 * Build script for the Obsidian plugin.
 * Handles copying plugin files, configuration, and executables to the dist directory.
 * Also zips plugin files for release/BRAT uploads.
 * Usage: node build-plugin.mjs
 */

/**
 * Main build process:
 * - Ensures dist directory exists
 * - Copies required plugin/config/prompt files
 * - Preserves executables from CI builds
 * - Verifies build outputs
 * - Zips plugin files for release
 */

const distRoot = resolve('./dist');
const currentDir = process.cwd();

console.log('🔨 Building Obsidian plugin...');
console.log(`   Plugin source: ${currentDir}`);
console.log(`   Output directory: ${distRoot}`);

// Ensure dist directory exists
if (!existsSync(distRoot)) {
    console.log('📁 Creating dist directory...');
    mkdirSync(distRoot, { recursive: true });
}


// Copy required plugin files (excluding main.js which is built by esbuild)
const pluginFiles = [
    { src: 'manifest.json', dest: 'manifest.json', required: true },
    { src: 'styles.css', dest: 'styles.css', required: true },
    { src: 'default-config.json', dest: 'default-config.json', required: true },
    // Add metadata-schema.yml from config folder
    { src: '../config/metadata-schema.yml', dest: 'metadata-schema.yml', required: true },
    // Add BaseBlockTemplate.yml from config folder
    { src: '../config/BaseBlockTemplate.yml', dest: 'BaseBlockTemplate.yml', required: true },
    // Add chunk_summary_prompt.md from prompts folder
    { src: '../prompts/chunk_summary_prompt.md', dest: 'chunk_summary_prompt.md', required: true },
    // Add final_summary_prompt.md from prompts folder
    { src: '../prompts/final_summary_prompt.md', dest: 'final_summary_prompt.md', required: true }
];

console.log('📋 Copying plugin files...');
import { fileURLToPath } from 'url';
const moduleDir = fileURLToPath(new URL('.', import.meta.url));

for (const file of pluginFiles) {
    let srcPath;
    // Use absolute paths for files outside plugin folder
    if (file.src.startsWith('../config/')) {
        srcPath = resolve(moduleDir, '../../config/', file.src.replace('../config/', ''));
    } else if (file.src.startsWith('../prompts/')) {
        srcPath = resolve(moduleDir, '../../prompts/', file.src.replace('../prompts/', ''));
    } else {
        srcPath = join(currentDir, file.src);
    }
    const destPath = join(distRoot, file.dest);

    if (existsSync(srcPath)) {
        copyFileSync(srcPath, destPath);
        console.log(`   ✅ ${file.src} → dist/${file.dest}`);
    } else if (file.required) {
        console.error(`   ❌ Required file missing: ${file.src}`);
        process.exit(1);
    } else {
        console.log(`   ⏭️  ${file.src} (will be created by esbuild)`);
    }
}

// Handle executables - ensure they're preserved in the dist directory
console.log('🔍 Processing executables...');
try {
    let executables = [];
    
    if (existsSync(distRoot)) {
        const files = readdirSync(distRoot);
        executables = files.filter(f =>
            f.startsWith('na-') &&
            (f.endsWith('.exe') || (!f.includes('.') && f.includes('-')))
        );
    }
    
    // If no executables found in dist, check the root dist directory
    if (executables.length === 0) {
        const rootDistDir = resolve('../../dist');
        if (existsSync(rootDistDir)) {
            console.log('   📂 Copying executables from root dist directory...');
            const rootDistFiles = readdirSync(rootDistDir);
            const rootExecutables = rootDistFiles.filter(f => 
                f.startsWith('na-') && 
                (f.endsWith('.exe') || (!f.includes('.') && f.includes('-')))
            );
            
            for (const exe of rootExecutables) {
                const srcPath = join(rootDistDir, exe);
                const destPath = join(distRoot, exe);
                
                copyFileSync(srcPath, destPath);
                console.log(`   ✅ ${exe} (from root dist)`);
                executables.push(exe);
            }
        }
    }

    if (executables.length > 0) {
        console.log(`   ✅ Found ${executables.length} executables in dist:`);
        executables.forEach(exe => {
            const exePath = join(distRoot, exe);
            if (existsSync(exePath)) {
                console.log(`      ✅ ${exe} (available in dist)`);
            } else {
                console.log(`      ⚠️  ${exe} (missing from dist)`);
            }
        });
    } else {
        console.log('   ℹ️  No executables found in dist or publish directories');
        console.log('   ℹ️  Run dotnet publish to generate executables');
    }
} catch (error) {
    console.log(`   ⚠️  Could not process executables: ${error.message}`);
}

// Verify build outputs
console.log('🔍 Verifying build outputs...');
const requiredOutputs = ['manifest.json', 'styles.css', 'default-config.json'];
let allPresent = true;

for (const file of requiredOutputs) {
    const filePath = join(distRoot, file);
    if (existsSync(filePath)) {
        console.log(`   ✅ ${file}`);
    } else {
        console.log(`   ❌ ${file} missing`);
        allPresent = false;
    }
}

// Check for main.js (might be created by esbuild after this script)
const mainJsPath = join(distRoot, 'main.js');
if (existsSync(mainJsPath)) {
    console.log(`   ✅ main.js`);
} else {
    console.log(`   ⏳ main.js (should be created by esbuild)`);
}

if (allPresent) {
    console.log('🎉 Plugin build completed successfully!');

    // Show final dist contents
    console.log('\n📦 Final dist directory contents:');
    try {
        const distFiles = readdirSync(distRoot);
        distFiles.sort().forEach(file => {
            console.log(`   - ${file}`);
        });
    } catch (error) {
        console.log(`   Could not list dist contents: ${error.message}`);
    }

    // --- Create asset manifest for dynamic file discovery ---
    try {
        console.log('📝 Creating asset manifest...');
        
        // List of files to include in the manifest
        const filesToManifest = [
            'BaseBlockTemplate.yml',
            'chunk_summary_prompt.md',
            'default-config.json',
            'final_summary_prompt.md',
            'main.js',
            'manifest.json',
            'metadata-schema.yml',
            'styles.css',
        ];
        
        // Add any executables that exist in dist
        const distFiles = readdirSync(distRoot);
        const executables = distFiles.filter(f => 
            f.startsWith('na-') && 
            (f.endsWith('.exe') || (!f.includes('.') && f.includes('-')))
        );
        filesToManifest.push(...executables);
        
        // Only include files that exist
        const manifestFilesPresent = filesToManifest.filter(f => existsSync(join(distRoot, f)));
        
        const manifest = {
            version: "1.0.0",
            files: manifestFilesPresent
        };
        const manifestPath = join(distRoot, 'asset-manifest.json');
        writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));
        console.log(`   ✅ Created asset-manifest.json with ${manifestFilesPresent.length} files`);
    } catch (err) {
        console.warn('   ⚠️  Could not create asset manifest:', err.message);
    }

    // --- Zip plugin files for BRAT and release uploads ---
    try {
        // Only require standard Node.js modules
        const { execSync } = await import('child_process');
        const zipName = 'notebook-automation-obsidian-plugin.zip';
        const zipPath = join(distRoot, zipName);
        // List of files to include in the zip
        const filesToZip = [
            'BaseBlockTemplate.yml',
            'chunk_summary_prompt.md',
            'default-config.json',
            'final_summary_prompt.md',
            'main.js',
            'manifest.json',
            'metadata-schema.yml',
            'styles.css',
        ];
        
        // Add any executables that exist in dist
        const distFiles = readdirSync(distRoot);
        const executables = distFiles.filter(f => 
            f.startsWith('na-') && 
            (f.endsWith('.exe') || (!f.includes('.') && f.includes('-')))
        );
        filesToZip.push(...executables);
        
        // Only include files that exist
        const filesPresent = filesToZip.filter(f => existsSync(join(distRoot, f)));
        if (filesPresent.length === 0) {
            throw new Error('No plugin files found to zip.');
        }
        // Build the zip command (cross-platform)
        // On Windows, use PowerShell Compress-Archive; on others, use zip
        let zipCmd;
        if (process.platform === 'win32') {
            // Use PowerShell Compress-Archive with explicit module import
            const filesArg = filesPresent.map(f => `'${join(distRoot, f)}'`).join(',');
            zipCmd = `pwsh -Command "Import-Module Microsoft.PowerShell.Archive -Force; Compress-Archive -Path ${filesArg} -DestinationPath '${zipPath}' -Force"`;
        } else {
            // Use zip CLI
            const filesArg = filesPresent.map(f => `'${f}'`).join(' ');
            zipCmd = `cd '${distRoot}' && zip -r '${zipName}' ${filesArg}`;
        }
        console.log(`\n📦 Creating plugin zip for release/BRAT: ${zipPath}`);
        execSync(zipCmd, { stdio: 'inherit' });
        if (existsSync(zipPath)) {
            console.log(`   ✅ Created ${zipName} in dist/`);
        } else {
            console.error(`   ❌ Failed to create ${zipName}`);
        }
    } catch (err) {
        console.error('   ⚠️  Could not create plugin zip:', err.message);
    }
} else {
    console.error('❌ Plugin build incomplete - some required files are missing');
    process.exit(1);
}
