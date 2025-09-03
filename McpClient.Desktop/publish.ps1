[string] $outputDir = "D:\workspace\output\win-x64"
[string] $zipPath   = "D:\workspace\output\win-x64.zip"

Remove-Item -Path $outputDir -Force -Recurse -ErrorAction SilentlyContinue
dotnet publish --nologo --configuration Release --self-contained true --runtime win-x64 --output $outputDir

# make a zip file
Write-Host "Compressing output..."
if (Test-Path $zipPath)   { Remove-Item -Path $zipPath   -Force }
Compress-Archive -Path "$outputDir\*" -DestinationPath $zipPath -Force