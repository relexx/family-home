#!/usr/bin/env bash
set -euo pipefail

# Named Volumes gehören nach dem Anlegen root
sudo chown -R vscode:vscode /home/vscode/.nuget /home/vscode/.claude

dotnet --list-sdks
dotnet workload list

# Kein Restore hier: Der erste Build bzw. scripts/security-gate.sh restauriert im Locked Mode

echo
echo "Fertig. Nützliche Befehle:"
echo "  dotnet run --project src/FamilyHome.Server            # Server auf http://localhost:5080"
echo "  dotnet build src/FamilyHome.Client.Android -c Debug   # APK bauen"
echo "  dotnet run --project src/FamilyHome.Client.Desktop    # Desktop-Variante (im noVNC-Desktop, Port 6080)"
