#!/bin/sh
# Launcher for AI Nexus Client

HERE="$(dirname "$(readlink -f "$0")")"

# Run main executable from /usr/lib/ai-nexus-client
exec /usr/lib/ai-nexus-client/McpClient.Desktop "$@"
