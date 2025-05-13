# deploy_templater_scripts.ps1
# Copies all scripts from the repo Templater folder to your Obsidian Templater scripts folder.

$repoScripts = "d:\repos\mba-notebook-automation\Templater"
$obsidianTemplaterScripts = "C:\Users\danshue.REDMOND\MBA\.obsidian\scripts"

Write-Host "Copying scripts from $repoScripts to $obsidianTemplaterScripts ..."
Copy-Item -Path "$repoScripts\*.js" -Destination $obsidianTemplaterScripts -Force
Write-Host "Done."
