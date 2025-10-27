#!/bin/bash

set -e

# Configurable variables
APP_NAME="ai-nexus-client"
APP_DISPLAY_NAME="AI Nexus Client"
VERSION="0.0.2"
RUNTIME="linux-x64"
MAIN_EXECUTABLE="McpClient.Desktop"

# Output paths
WORKSPACE="${HOME}/workspace/output"
PUBLISH_DIR="${WORKSPACE}/${RUNTIME}"
STAGING_DIR="${WORKSPACE}/staging_folder"

# Clean-up output directory if it exists
if [ -d "$PUBLISH_DIR" ]; then
	rm -rf "$PUBLISH_DIR"
fi
if [ -d "$STAGING_DIR" ]; then
	rm -rf "$STAGING_DIR"
fi

# .NET publish (self-contained)
dotnet publish "./McpClient.Desktop.csproj" \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime $RUNTIME \
  --output "$PUBLISH_DIR"

# Staging directory structure
mkdir -p "$STAGING_DIR/DEBIAN"
mkdir -p "$STAGING_DIR/usr/bin"
mkdir -p "$STAGING_DIR/usr/lib/$APP_NAME"
mkdir -p "$STAGING_DIR/usr/share/applications"
mkdir -p "$STAGING_DIR/usr/share/pixmaps"
mkdir -p "$STAGING_DIR/usr/share/icons/hicolor/scalable/apps"

# Debian control file
cp ./Debian/control "$STAGING_DIR/DEBIAN"

# Starter script
cp ./Debian/ainexusclient.sh "$STAGING_DIR/usr/bin/ainexusclient"
chmod +x "$STAGING_DIR/usr/bin/ainexusclient"

# App binaries
cp -f -a "$PUBLISH_DIR/." "$STAGING_DIR/usr/lib/$APP_NAME/"
chmod -R a+rX "$STAGING_DIR/usr/lib/$APP_NAME/"
chmod +x "$STAGING_DIR/usr/lib/$APP_NAME/$MAIN_EXECUTABLE"

# Desktop shortcut
cp ./Debian/AI-Nexus-Client.desktop "$STAGING_DIR/usr/share/applications/AI-Nexus-Client.desktop"

# Desktop icon
cp ./logo.png "$STAGING_DIR/usr/share/pixmaps/ainexusclient.png"

# Hicolor SVG icon (optional)
cp ./logo.svg "$STAGING_DIR/usr/share/icons/hicolor/scalable/apps/ainexusclient.svg"

# Make .deb file (output to $WORKSPACE)
dpkg-deb --root-owner-group --build "$STAGING_DIR" "$WORKSPACE/${APP_NAME}_${VERSION}_amd64.deb"

echo "Debian package created at: $WORKSPACE/${APP_NAME}_${VERSION}_amd64.deb"
