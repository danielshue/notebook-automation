/**
 * Version bump script for the Obsidian plugin.
 * Updates manifest.json and versions.json with the target version and minAppVersion.
 * Usage: node version-bump.mjs
 */

import { readFileSync, writeFileSync } from "fs";

/**
 * Main version bump process:
 * - Reads target version from npm_package_version
 * - Updates manifest.json version
 * - Updates versions.json with minAppVersion
 */

const targetVersion = process.env.npm_package_version;

// read minAppVersion from manifest.json and bump version to target version
let manifest = JSON.parse(readFileSync("manifest.json", "utf8"));
const { minAppVersion } = manifest;
manifest.version = targetVersion;
writeFileSync("manifest.json", JSON.stringify(manifest, null, "\t"));

// update versions.json with target version and minAppVersion from manifest.json
let versions = JSON.parse(readFileSync("versions.json", "utf8"));
versions[targetVersion] = minAppVersion;
writeFileSync("versions.json", JSON.stringify(versions, null, "\t"));
