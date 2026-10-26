using FamilyHome.Server.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyHome.Server.Security;

/// <summary>
/// Lädt beim Start die Familien-CA aus der DB oder legt sie bei der Ersteinrichtung an.
/// Der private Schlüssel ist mit Data Protection verschlüsselt; das öffentliche Zertifikat wird für Caddy exportiert.
/// </summary>
public sealed class CertificateAuthorityProvider(
    IServiceScopeFactory scopes,
    IDataProtectionProvider dataProtection,
    IOptions<SecurityOptions> options,
    TimeProvider time,
    ILogger<CertificateAuthorityProvider> logger) : IDisposable
{
    private const string ProtectorPurpose = "FamilyHome.CertificateAuthority.v1";
    private FamilyCertificateAuthority? _current;

    public FamilyCertificateAuthority Current =>
        _current ?? throw new InvalidOperationException("Familien-CA ist noch nicht initialisiert.");

    /// <summary>Nur für Tests: eine vorhandene CA verwenden, ohne Datenbank.</summary>
    public void Use(FamilyCertificateAuthority authority) => _current = authority;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_current is not null) return;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyHomeDbContext>();
        var protector = dataProtection.CreateProtector(ProtectorPurpose);

        var record = await db.CertificateAuthority.SingleOrDefaultAsync(ct);
        if (record is null)
        {
            var name = await db.FamilySettings.Select(f => f.Name).SingleOrDefaultAsync(ct) ?? "Family";
            _current = FamilyCertificateAuthority.CreateNew(name, time);
            db.CertificateAuthority.Add(new CertificateAuthorityRecord
            {
                CertificatePem = _current.CertificatePem,
                PrivateKeyProtected = protector.Protect(_current.ExportPrivateKeyPem()),
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Neue Familien-CA angelegt.");
        }
        else
        {
            _current = FamilyCertificateAuthority.Load(record.CertificatePem, protector.Unprotect(record.PrivateKeyProtected), time);
        }

        var exportPath = options.Value.CaExportPath;
        if (!string.IsNullOrEmpty(exportPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
            await File.WriteAllTextAsync(exportPath, _current.CertificatePem, ct);
        }
    }

    public void Dispose() => _current?.Dispose();
}

public sealed class SecurityOptions
{
    public const string Section = "Security";

    /// <summary>
    /// Netze (CIDR), aus denen der Zertifikat-Header des Reverse Proxys akzeptiert wird.
    /// Leer = Header wird immer verworfen (außer Loopback in der Entwicklung).
    /// </summary>
    public string[] TrustedProxyNetworks { get; set; } = [];

    /// <summary>Pfad, an den das öffentliche CA-Zertifikat für Caddy geschrieben wird (geteiltes Volume).</summary>
    public string? CaExportPath { get; set; }

    /// <summary>SHA-256-Fingerabdrücke der offiziellen App-Signaturzertifikate (für assetlinks.json / Passkeys).</summary>
    public string[] AppSigningCertFingerprints { get; set; } = [];

    public string AppPackageName { get; set; } = "app.familyhome";

    /// <summary>Ältere Apps erhalten 426 und müssen aktualisieren.</summary>
    public string MinAppVersion { get; set; } = "0.1.0";
}
