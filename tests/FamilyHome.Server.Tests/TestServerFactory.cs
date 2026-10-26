using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FamilyHome.Server.Data;
using FamilyHome.Server.Security;
using FamilyHome.Shared.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FamilyHome.Server.Tests;

/// <summary>
/// Startet den Server mit vorbereiteter Familien-CA und SQLite-In-Memory statt PostgreSQL (Umgebung "Testing").
/// Simuliert Caddy: Anfragen aus <see cref="ProxyAddress"/> gelten als vom vertrauenswürdigen Proxy.
/// </summary>
public sealed class TestServerFactory : WebApplicationFactory<Program>
{
    public const string ProxyAddress = "10.99.0.2";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public FamilyCertificateAuthority Ca { get; } = FamilyCertificateAuthority.CreateNew("Test", TimeProvider.System);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:FamilyHome", "Host=unused;Password=test-only");
        builder.UseSetting("Security:TrustedProxyNetworks:0", ProxyAddress + "/32");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FamilyHomeDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FamilyHomeDbContext>>();
            services.AddDbContext<FamilyHomeDbContext>(o => o.UseSqlite(_connection));

            services.AddSingleton(sp =>
            {
                var provider = ActivatorUtilities.CreateInstance<CertificateAuthorityProvider>(sp);
                provider.Use(Ca);
                return provider;
            });
        });
    }

    public void EnsureDatabase()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<FamilyHomeDbContext>().Database.EnsureCreated();
    }

    /// <summary>Legt Account + Gerät an und liefert das passende, von der Familien-CA ausgestellte Zertifikat.</summary>
    public X509Certificate2 CreateRegisteredDevice(Role role = Role.Parent, bool active = true)
    {
        EnsureDatabase();
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var csr = new CertificateRequest("CN=x", key, HashAlgorithmName.SHA256).CreateSigningRequestPem();
        var deviceId = Guid.CreateVersion7();
        var cert = Ca.IssueDeviceCertificate(csr, deviceId);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyHomeDbContext>();
        var account = new Account { DisplayName = "Test", Role = role, PersonId = Guid.CreateVersion7(), CreatedAt = DateTimeOffset.UnixEpoch };
        db.Accounts.Add(account);
        db.Devices.Add(new Device
        {
            Id = deviceId,
            AccountId = account.Id,
            Name = "Testgerät",
            CertThumbprint = cert.GetCertHashString(HashAlgorithmName.SHA256),
            CertNotAfter = new DateTimeOffset(cert.NotAfter.ToUniversalTime(), TimeSpan.Zero),
            IsActive = active,
        });
        db.SaveChanges();
        return cert;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            Ca.Dispose();
        }
    }
}
