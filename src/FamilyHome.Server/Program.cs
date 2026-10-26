using FamilyHome.Server.Api;
using FamilyHome.Server.Auth;
using FamilyHome.Server.Data;
using FamilyHome.Server.Dav;
using FamilyHome.Server.Security;
using FamilyHome.Server.Sync;
using FamilyHome.Shared.Sync;
using Microsoft.EntityFrameworkCore;

// Healthcheck-Modus für das chiseled Image (kein curl/Shell vorhanden): docker HEALTHCHECK ruft "dotnet FamilyHome.Server.dll --healthcheck"
if (args.Contains("--healthcheck"))
{
    // Kein DI-Container im Healthcheck-Modus, daher bewusst ohne IHttpClientFactory
#pragma warning disable RS0030
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
#pragma warning restore RS0030
    try { return (await http.GetAsync(new Uri("http://localhost:8080/health"))).IsSuccessStatusCode ? 0 : 1; }
    catch (HttpRequestException) { return 1; }
    catch (TaskCanceledException) { return 1; }
}

var builder = WebApplication.CreateBuilder(args);
builder.AddFamilyHomeSecurity();
builder.AddDeviceAuthentication();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<FamilyHomeDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("FamilyHome")));
builder.Services.AddDataProtection();
builder.Services.AddSingleton<CertificateAuthorityProvider>();
builder.Services.AddSignalR();

builder.Services.AddScoped<SyncService>();
builder.Services.AddHostedService<DavSyncWorker>();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Entwicklung: Schema direkt anlegen. Sobald das Modell stabil ist, auf EF-Migrationen umstellen.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<FamilyHomeDbContext>().Database.EnsureCreatedAsync();
}

await app.Services.GetRequiredService<CertificateAuthorityProvider>().InitializeAsync();

// Reihenfolge ist sicherheitsrelevant: Header-Filter (prüft die echte Proxy-Adresse) → Forwarded Headers → Zertifikat
app.UseTrustedProxyCertificateHeaderFilter();
app.UseFamilyHomeSecurity();
app.UseDeviceAuthentication();

app.MapHealthChecks("/health");
app.MapDeviceEndpoints();
app.MapSyncEndpoints();
app.MapHub<SyncHub>(SyncHubMethods.HubPath);

await app.RunAsync();
return 0;

public partial class Program;
