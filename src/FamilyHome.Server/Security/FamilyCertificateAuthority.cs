using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FamilyHome.Server.Security;

/// <summary>
/// Eigene Zertifizierungsstelle des Familien-Servers für Gerätezertifikate (docs/SICHERHEIT.md Kap. 2.1).
/// Stellt nur End-Entity-Zertifikate für TLS-Client-Authentifizierung aus und prüft sie.
/// </summary>
public sealed class FamilyCertificateAuthority : IDisposable
{
    public static readonly TimeSpan DeviceCertificateLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan CaLifetime = TimeSpan.FromDays(3650);
    private static readonly Oid ClientAuthOid = new("1.3.6.1.5.5.7.3.2");

    private readonly X509Certificate2 _caCertificate;
    private readonly ECDsa _caKey;
    private readonly TimeProvider _time;

    private FamilyCertificateAuthority(X509Certificate2 caCertificate, ECDsa caKey, TimeProvider time)
    {
        _caCertificate = caCertificate;
        _caKey = caKey;
        _time = time;
    }

    /// <summary>Öffentliches CA-Zertifikat (für Caddy und den Einladungs-QR).</summary>
    public X509Certificate2 Certificate => _caCertificate;

    public string CertificatePem => _caCertificate.ExportCertificatePem();

    public static FamilyCertificateAuthority CreateNew(string familyName, TimeProvider time)
    {
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            new X500DistinguishedName($"CN={Sanitize(familyName)} FamilyHome Device CA"), key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: true, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

        var now = time.GetUtcNow();
        var self = request.CreateSelfSigned(now.AddMinutes(-5), now.Add(CaLifetime));
        var publicOnly = X509CertificateLoader.LoadCertificate(self.Export(X509ContentType.Cert));
        self.Dispose();
        return new FamilyCertificateAuthority(publicOnly, key, time);
    }

    public static FamilyCertificateAuthority Load(string certificatePem, string privateKeyPem, TimeProvider time)
    {
        var key = ECDsa.Create();
        key.ImportFromPem(privateKeyPem);
        return new FamilyCertificateAuthority(X509Certificate2.CreateFromPem(certificatePem), key, time);
    }

    /// <summary>Nur zum verschlüsselten Speichern (Data Protection) gedacht.</summary>
    public string ExportPrivateKeyPem() => _caKey.ExportPkcs8PrivateKeyPem();

    /// <summary>
    /// Stellt aus einer CSR des Geräts ein kurzlebiges Client-Zertifikat aus. Subject und Erweiterungen der CSR
    /// werden ignoriert; der Server bestimmt alles selbst. Nur ECDSA P-256 wird akzeptiert.
    /// </summary>
    public X509Certificate2 IssueDeviceCertificate(string csrPem, Guid deviceId)
    {
        var csr = CertificateRequest.LoadSigningRequestPem(
            csrPem, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.Default, RSASignaturePadding.Pkcs1);

        using (var deviceKey = csr.PublicKey.GetECDsaPublicKey()
            ?? throw new CryptographicException("Nur ECDSA-Schlüssel sind für Geräte zulässig."))
        {
            if (deviceKey.KeySize != 256)
                throw new CryptographicException("Nur ECDSA P-256 ist für Geräte zulässig.");
        }

        var request = new CertificateRequest(new X500DistinguishedName($"CN={deviceId:D}"), csr.PublicKey, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([ClientAuthOid], critical: true));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(_caCertificate, includeKeyIdentifier: true, includeIssuerAndSerial: false));

        var now = _time.GetUtcNow();
        var serial = RandomNumberGenerator.GetBytes(16);
        serial[0] &= 0x7F; // positive Seriennummer

        var generator = X509SignatureGenerator.CreateForECDsa(_caKey);
        return request.Create(_caCertificate.SubjectName, generator, now.AddMinutes(-5), now.Add(DeviceCertificateLifetime), serial);
    }

    /// <summary>Prüft, ob ein Zertifikat von dieser CA für Client-Authentifizierung ausgestellt wurde und gültig ist.</summary>
    public bool Validate(X509Certificate2 certificate)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(_caCertificate);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // Sperrung über die Gerätetabelle
        chain.ChainPolicy.ApplicationPolicy.Add(ClientAuthOid);
        chain.ChainPolicy.VerificationTime = _time.GetUtcNow().UtcDateTime;
        chain.ChainPolicy.VerificationTimeIgnored = false;

        return chain.Build(certificate)
            && chain.ChainElements.Count == 2
            && chain.ChainElements[^1].Certificate.RawDataMemory.Span.SequenceEqual(_caCertificate.RawDataMemory.Span);
    }

    private static string Sanitize(string value) =>
        new([.. value.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-').Take(48)]);

    public void Dispose()
    {
        _caCertificate.Dispose();
        _caKey.Dispose();
    }
}
