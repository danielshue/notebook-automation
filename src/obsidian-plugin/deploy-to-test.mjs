#!/usr/bin/env node

import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "fs";
import { resolve, join } from "path";

/**
 * Deploy script for the Obsidian plugin to test vault.
 * Copies built plugin files and executables to the test vault plugins directory.
 * Also updates community-plugins.json to enable the plugin.
 * Usage: node deploy-to-test.mjs
 */

/**
 * Main deploy process:
 * - Checks for dist and test vault directories
 * - Copies plugin files and executables
 * - Updates community-plugins.json
 * - Shows final plugin directory contents
 */

const distRoot = resolve('./dist');
const testVaultPath = resolve('../../tests/obsidian-vault/Obsidian Vault Test/.obsidian/plugins');
const pluginName = 'notebook-automation';
const pluginDir = join(testVaultPath, pluginName);

console.log('🚀 Deploying plugin to test vault...');
console.log(`   Source: ${distRoot}`);
console.log(`   Test vault: ${testVaultPath}`);
console.log(`   Plugin directory: ${pluginDir}`);

// Check if dist directory exists
if (!existsSync(distRoot)) {
    console.error('❌ Dist directory does not exist. Run "npm run build" first.');
    process.exit(1);
}

// Check if test vault exists
if (!existsSync(testVaultPath)) {
    console.error('❌ Test vault plugins directory does not exist:');
    console.error(`   ${testVaultPath}`);
    process.exit(1);
}

// Create plugin directory
if (!existsSync(pluginDir)) {
    console.log('📁 Creating plugin directory...');
    mkdirSync(pluginDir, { recursive: true });
}

// Files to copy to the plugin directory
const pluginFiles = [
    'main.js',
    'manifest.json',
    'styles.css',
    'default-config.json',
    'metadata-schema.yml',
    'BaseBlockTemplate.yml',
    'chunk_summary_prompt.md',
    'final_summary_prompt.md'
];

// Copy plugin files
console.log('📋 Copying plugin files...');
let copiedCount = 0;

for (const file of pluginFiles) {
    const srcPath = join(distRoot, file);
    const destPath = join(pluginDir, file);

    if (existsSync(srcPath)) {
        copyFileSync(srcPath, destPath);
        console.log(`   ✅ ${file}`);
        copiedCount++;
    } else {
        console.log(`   ❌ ${file} (missing from dist)`);
    }
}

// Copy any executables if they exist
console.log('🔍 Checking for executables...');
try {
    const distFiles = readdirSync(distRoot);
    let executables = distFiles.filter(f => 
        f.startsWith('na-') && 
        (f.endsWith('.exe') || (!f.includes('.') && f.includes('-')))
    );

    // If no executables found in dist, check the publish directory
    if (executables.length === 0) {
        const publishRoot = resolve('../../publish');
        if (existsSync(publishRoot)) {
            console.log('   📂 Checking publish directory for executables...');
            const publishDirs = readdirSync(publishRoot);
            
            for (const platformDir of publishDirs) {
                const platformPath = join(publishRoot, platformDir);
                if (existsSync(platformPath)) {
                    const platformFiles = readdirSync(platformPath);
                    const platformExecutables = platformFiles.filter(f => 
                        f === 'na.exe' || f === 'na' || f.startsWith('na-')
                    );
                    
                    for (const exe of platformExecutables) {
                        const srcPath = join(platformPath, exe);
                        // Rename to include platform info for clarity
                        const destName = exe === 'na.exe' ? `na-${platformDir}.exe` : 
                                       exe === 'na' ? `na-${platformDir}` : exe;
                        const destPath = join(distRoot, destName);
                        
                        copyFileSync(srcPath, destPath);
                        console.log(`   ✅ ${exe} → ${destName} (from ${platformDir})`);
                        copiedCount++;
                        
                        // Also copy to plugin directory
                        const pluginDestPath = join(pluginDir, destName);
                        copyFileSync(srcPath, pluginDestPath);
                        executables.push(destName);
                    }
                }
            }
        }
    }

    if (executables.length > 0) {
        console.log(`   Found ${executables.length} executables:`);
        executables.forEach(exe => {
            // If not already copied from publish, copy from dist
            const srcPath = join(distRoot, exe);
            const destPath = join(pluginDir, exe);
            if (existsSync(srcPath) && !existsSync(destPath)) {
                copyFileSync(srcPath, destPath);
                console.log(`   ✅ ${exe} (from dist)`);
                copiedCount++;
            }
        });
    } else {
        console.log('   ℹ️  No executables found in dist or publish directories');
    }
} catch (error) {
    console.log(`   ⚠️  Could not check for executables: ${error.message}`);
}

// Update community-plugins.json to enable the plugin
const communityPluginsPath = join(testVaultPath, '../community-plugins.json');
console.log('🔧 Updating community-plugins.json...');

try {
    let communityPlugins = [];
    
    if (existsSync(communityPluginsPath)) {
        const content = readFileSync(communityPluginsPath, 'utf8');
        communityPlugins = JSON.parse(content);
    }
    
    // Add our plugin if it's not already there
    if (!communityPlugins.includes(pluginName)) {
        communityPlugins.push(pluginName);
        writeFileSync(communityPluginsPath, JSON.stringify(communityPlugins, null, 2));
        console.log(`   ✅ Added ${pluginName} to community-plugins.json`);
    } else {
        console.log(`   ℹ️  ${pluginName} already in community-plugins.json`);
    }
} catch (error) {
    console.log(`   ⚠️  Could not update community-plugins.json: ${error.message}`);
}

// Show final plugin directory contents
console.log('\n📦 Plugin directory contents:');
try {
    const pluginFiles = readdirSync(pluginDir);
    pluginFiles.sort().forEach(file => {
        console.log(`   - ${file}`);
    });
} catch (error) {
    console.log(`   Could not list plugin directory: ${error.message}`);
}

if (copiedCount > 0) {
    console.log(`\n🎉 Successfully deployed ${copiedCount} files to test vault!`);
    console.log('\n📝 Next steps:');
    console.log('   1. Open the test vault in Obsidian');
    console.log('   2. Go to Settings > Community plugins');
    console.log('   3. Enable the "Notebook Automation" plugin');
    console.log('   4. The plugin should now be available in the test vault');
} else {
    console.error('\n❌ No files were copied. Check the build output.');
    process.exit(1);
}
