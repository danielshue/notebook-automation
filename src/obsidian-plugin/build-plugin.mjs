#!/usr/bin/env node

import { readFileSync, writeFileSync, copyFileSync, existsSync, mkdirSync, readdirSync } from "fs";
import { resolve, join } from "path";

/**
 * Build script for the Obsidian plugin
 * This script handles:
 * 1. Copying plugin files (manifest.json, styles.css, main.js) to the root dist directory
 * 2. Copying configuration files
 * 3. Ensuring executables from CI builds are preserved in the dist directory
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
    { src: 'default-config.json', dest: 'default-config.json', required: true }
];

console.log('📋 Copying plugin files...');
for (const file of pluginFiles) {
    const srcPath = join(currentDir, file.src);
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
    if (existsSync(distRoot)) {
        const files = readdirSync(distRoot);
        const executables = files.filter(f =>
            f.startsWith('na-') &&
            (f.endsWith('.exe') || (!f.includes('.') && f.includes('-')))
        );

        if (executables.length > 0) {
            console.log(`   ✅ Found ${executables.length} executables in dist:`);
            executables.forEach(exe => {
                const exePath = join(distRoot, exe);
                if (existsSync(exePath)) {
                    console.log(`      ✅ ${exe} (preserved in dist)`);
                } else {
                    console.log(`      ⚠️  ${exe} (missing from dist)`);
                }
            });
        } else {
            console.log('   ℹ️  No executables found (normal for local development)');
            console.log('   ℹ️  Executables will be added by CI workflow');
        }
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
} else {
    console.error('❌ Plugin build incomplete - some required files are missing');
    process.exit(1);
}
