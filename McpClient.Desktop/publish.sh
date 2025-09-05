#!/bin/bash
output_dir="$HOME/workspace/output/linux-x64"
zip_path="$HOME/workspace/output/linux-x64.zip"

# Remove output directory if it exists
if [ -d "$output_dir" ]; then
    rm -rf "$output_dir"
fi

# Publish the project (assumes the .csproj is in the current directory)
dotnet publish \
    --nologo \
    --configuration Release \
    --self-contained true \
    --runtime linux-x64 \
    --output "$output_dir"

# make a zip file
echo "Compressing output..."
cd "$output_dir"
zip -9 -r "$zip_path" ./*