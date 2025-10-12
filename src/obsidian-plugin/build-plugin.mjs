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

// Use single root-level dist directory (two levels up from plugin folder)
const distRoot = resolve('../../dist');
const currentDir = process.cwd();

console.log('🔨 Building Obsidian plugin...');
console.log(`   Plugin source: ${currentDir}`);
console.log(`   Output directory (root dist): ${distRoot}`);

// Ensure root dist directory exists
if (!existsSync(distRoot)) {
    console.log('📁 Creating root dist directory...');
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
    // Purge any legacy osx-named executables to enforce canonical naming (macos)
    try {
        if (existsSync(distRoot)) {
            const prePurge = readdirSync(distRoot).filter(f => f.startsWith('na-osx-'));
            if (prePurge.length > 0) {
                console.log(`   🧹 Removing legacy executables: ${prePurge.join(', ')}`);
                for (const legacy of prePurge) {
                    try {
                        const p = join(distRoot, legacy);
                        // Use fs.rmSync via dynamic import to avoid adding at top
                        const { rmSync } = await import('fs');
                        rmSync(p, { force: true });
                    } catch (err) {
                        console.warn(`   ⚠️ Failed to remove legacy executable ${legacy}: ${err.message}`);
                    }
                }
            }
        }
    } catch (purgeErr) {
        console.warn(`   ⚠️ Legacy purge encountered an issue: ${purgeErr.message}`);
    }
    
    if (existsSync(distRoot)) {
        const files = readdirSync(distRoot);
        executables = files.filter(f =>
            f.startsWith('na-') &&
            (f.endsWith('.exe') || (!f.includes('.') && f.includes('-')))
        );
    }
    
    // If no executables found in dist, check the root dist directory
    // Root dist is authoritative; no secondary copy step required now

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
        // Final guard: ensure no legacy osx names remain after processing
        const lingeringLegacy = executables.filter(e => e.startsWith('na-osx-'));
        if (lingeringLegacy.length > 0) {
            console.error(`   ❌ Legacy executables still present after purge: ${lingeringLegacy.join(', ')}`);
            process.exit(1);
        }
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
            'checksums.json' // integrity file
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
        
        // Determine version from plugin manifest if available
        let manifestVersion = '0.0.0';
        try {
            const manifestJson = JSON.parse(readFileSync(join(distRoot, 'manifest.json'), 'utf8'));
            if (manifestJson?.version) manifestVersion = manifestJson.version;
        } catch { /* ignore */ }

        const manifest = {
            version: manifestVersion,
            generatedUtc: new Date().toISOString(),
            files: manifestFilesPresent
        };
        const manifestPath = join(distRoot, 'asset-manifest.json');
        writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));
        console.log(`   ✅ Created asset-manifest.json with ${manifestFilesPresent.length} files`);
    } catch (err) {
        console.warn('   ⚠️  Could not create asset manifest:', err.message);
    }

    // Note: Zip file creation removed - BRAT downloads individual files from asset-manifest.json
    // Individual file distribution is more efficient and allows platform-specific executable downloads
    
} else {
    console.error('❌ Plugin build incomplete - some required files are missing');
    process.exit(1);
}
