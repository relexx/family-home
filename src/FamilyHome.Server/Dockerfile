# Build-Kontext: Repository-Wurzel  →  docker build -f src/FamilyHome.Server/Dockerfile .
# Laufzeit: "chiseled" Ubuntu-Image (ohne Shell/Paketmanager), Nicht-Root (docs/SICHERHEIT.md Kap. 5.2)
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /src
COPY global.json nuget.config Directory.Build.props Directory.Packages.props BannedSymbols.txt .editorconfig ./
COPY src/FamilyHome.Shared/*.csproj src/FamilyHome.Shared/packages.lock.json src/FamilyHome.Shared/
COPY src/FamilyHome.Server/*.csproj src/FamilyHome.Server/packages.lock.json src/FamilyHome.Server/
RUN dotnet restore src/FamilyHome.Server/FamilyHome.Server.csproj --locked-mode
COPY src/FamilyHome.Shared/ src/FamilyHome.Shared/
COPY src/FamilyHome.Server/ src/FamilyHome.Server/
RUN dotnet publish src/FamilyHome.Server/FamilyHome.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled@sha256:c89a6a61f494f20a5ae302990e814627a8d91b332fd4a8e7c1707c2d4da9555d
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0
EXPOSE 8080
# chiseled-Images laufen standardmäßig als Nicht-Root-Benutzer "app" (UID 1654)
USER app
ENTRYPOINT ["dotnet", "FamilyHome.Server.dll"]
