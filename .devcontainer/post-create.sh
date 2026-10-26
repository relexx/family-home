#!/usr/bin/env bash
set -euo pipefail

# Named Volumes gehören nach dem Anlegen root
sudo chown -R vscode:vscode /home/vscode/.nuget /home/vscode/.claude

dotnet --list-sdks
dotnet workload list

# Restore inkl. Android-Kopfprojekt (lädt die Android-Packs beim ersten Mal)
dotnet restore FamilyHome.slnx || echo "WARN: restore fehlgeschlagen – siehe Ausgabe oben"

echo
echo "Fertig. Nützliche Befehle:"
echo "  dotnet run --project src/FamilyHome.Server            # Server auf http://localhost:5080"
echo "  dotnet build src/FamilyHome.App.Android -c Debug      # APK bauen"
echo "  dotnet run --project src/FamilyHome.App.Desktop       # Desktop-Variante (im noVNC-Desktop, Port 6080)"
