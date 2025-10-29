#!/bin/sh
# Launcher for AI Nexus Client

HERE="$(dirname "$(readlink -f "$0")")"

# Run main executable from /usr/lib/AINexusDesktop
exec /usr/lib/AINexusDesktop/McpClient.Desktop "$@"
