[string] $outputDir = "D:\workspace\output\win-x64"
[string] $zipPath   = "D:\workspace\output\win-x64.zip"

# clear output directory and build
Remove-Item -Path $outputDir -Force -Recurse -ErrorAction SilentlyContinue
dotnet publish --nologo --configuration Release --self-contained true --runtime win-x64 --output $outputDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed. Aborting."
    Read-Host
    exit 1
}

# copy MCP host files
[string] $nodeJsSourceDir = "E:\workspace\Phison_AI_Nexus_MCPHost\dist"
[string] $nodeJsDestinationDir = Join-Path $outputDir "McpNodeJs"
Remove-Item -Path $nodeJsDestinationDir -Force -Recurse -ErrorAction SilentlyContinue
if (Test-Path $nodeJsSourceDir) {
    Copy-Item -Path $nodeJsSourceDir -Destination $nodeJsDestinationDir -Recurse
    Write-Host "Copied MCP host files."
} else {
    Write-Warning "MCP host source directory not found: $nodeJsSourceDir"
}

# copy default MCP setting config
[string] $sourceConfigFile = Join-Path $PSScriptRoot "mcp_servers.config.json"
[string] $destinationConfigFile = Join-Path $nodeJsDestinationDir "mcp_servers.config.json"
if (Test-Path $sourceConfigFile) {
    Copy-Item -Path $sourceConfigFile -Destination $destinationConfigFile -Force
    Write-Host "Copied $sourceConfigFile to $destinationConfigFile"
} else {
    Write-Warning "Source config file not found: $sourceConfigFile"
}

# copy dispatcher backend files
[string] $backendSourceDir = "D:\workspace\output\McpBackend-win-x64"
[string] $backendDestinationDir = Join-Path $outputDir "\McpBackend"
Remove-Item -Path $backendDestinationDir -Force -Recurse -ErrorAction SilentlyContinue
if (Test-Path $backendSourceDir) {
    Copy-Item -Path $backendSourceDir -Destination $backendDestinationDir -Recurse
    Write-Host "Copied dispatcher backend files."
} else {
    Write-Warning "Backend source directory not found: $backendSourceDir"
}

# copy chat frontend web app
[string] $frontendSourceDir = "D:\tenny_lu\Documents\dist"
[string] $frontendDestinationDir = Join-Path $outputDir "\dist"
Remove-Item -Path $frontendDestinationDir -Force -Recurse -ErrorAction SilentlyContinue
if (Test-Path $frontendSourceDir) {
    Copy-Item -Path $frontendSourceDir -Destination $frontendDestinationDir -Recurse
    Write-Host "Copied chat frontend files."
} else {
    Write-Warning "Chat frontend source directory not found: $frontendSourceDir"
}

# make a zip file
Write-Host "Compressing output..."
if (Test-Path $zipPath)   { Remove-Item -Path $zipPath   -Force }
Compress-Archive -Path "$outputDir\*" -DestinationPath $zipPath -Force
Write-Host "Build and packaging complete! Output: $zipPath"
Read-Host