#!/bin/bash

set -e

# Configurable variables
APP_NAME="AINexusDesktop"
APP_DISPLAY_NAME="AI Nexus Desktop"
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
cp ./Debian/ai-nexus-desktop.sh "$STAGING_DIR/usr/bin/ai-nexus-desktop"
chmod +x "$STAGING_DIR/usr/bin/ai-nexus-desktop"

# App binaries
cp -f -a "$PUBLISH_DIR/." "$STAGING_DIR/usr/lib/$APP_NAME/"
chmod -R a+rX "$STAGING_DIR/usr/lib/$APP_NAME/"
chmod +x "$STAGING_DIR/usr/lib/$APP_NAME/$MAIN_EXECUTABLE"

##########################################
# 1. Copy NodeJs MCP host files
NODEJS_SRC="${HOME}/workspace/phison_ai_nexus_mcphost/dist"
NODEJS_DST="$STAGING_DIR/usr/lib/$APP_NAME/McpNodeJs"
rm -rf "$NODEJS_DST"
if [ -d "$NODEJS_SRC" ]; then
    cp -r "$NODEJS_SRC" "$NODEJS_DST"
    echo "Copied MCP host files."
else
    echo "Warning: NodeJs MCP host not found: $NODEJS_SRC"
fi

# 2. Copy default MCP setting config from project root
CONFIG_SRC="./mcp_servers.config.json"
CONFIG_DST="$NODEJS_DST/mcp_servers.config.json"
if [ -f "$CONFIG_SRC" ]; then
    cp "$CONFIG_SRC" "$CONFIG_DST"
    echo "Copied MCP config."
else
    echo "Warning: MCP config not found: $CONFIG_SRC"
fi

# 3. Copy dispatcher backend files
BACKEND_SRC="${HOME}/workspace/output/McpBackend/linux-x64"
BACKEND_DST="$STAGING_DIR/usr/lib/$APP_NAME/McpBackend"
rm -rf "$BACKEND_DST"
if [ -d "$BACKEND_SRC" ]; then
    cp -r "$BACKEND_SRC" "$BACKEND_DST"
    echo "Copied dispatcher backend files."
else
    echo "Warning: Backend source directory not found: $BACKEND_SRC"
fi

# 4. Copy chat frontend dist files
FRONTEND_SRC="${HOME}/workspace/chat_frontend/dist"
FRONTEND_DST="$STAGING_DIR/usr/lib/$APP_NAME/dist"
rm -rf "$FRONTEND_DST"
if [ -d "$FRONTEND_SRC" ]; then
    cp -r "$FRONTEND_SRC" "$FRONTEND_DST"
    echo "Copied chat frontend files."
else
    echo "Warning: Chat frontend source directory not found: $FRONTEND_SRC"
fi

# 5. Copy RAG dotnet server files
RAG_SRC="/path/to/RagBackend-linux-x64"          # <-- fill in
RAG_DST="$STAGING_DIR/usr/lib/$APP_NAME/RagBackend"
rm -rf "$RAG_DST"
if [ -d "$RAG_SRC" ]; then
    cp -r "$RAG_SRC" "$RAG_DST"
    echo "Copied RAG backend files."
else
    echo "Warning: RAG backend source directory not found: $RAG_SRC"
fi
##########################################

# Desktop shortcut
cp ./Debian/AI-Nexus-Desktop.desktop "$STAGING_DIR/usr/share/applications/AI-Nexus-Desktop.desktop"

# Desktop icon
cp ./logo.png "$STAGING_DIR/usr/share/pixmaps/ainexusclient.png"

# Hicolor SVG icon (optional)
cp ./logo.svg "$STAGING_DIR/usr/share/icons/hicolor/scalable/apps/ainexusclient.svg"

# Make .deb file (output to $WORKSPACE)
dpkg-deb --root-owner-group --build "$STAGING_DIR" "$WORKSPACE/${APP_NAME}_${VERSION}_amd64.deb"

echo "Debian package created at: $WORKSPACE/${APP_NAME}_${VERSION}_amd64.deb"
