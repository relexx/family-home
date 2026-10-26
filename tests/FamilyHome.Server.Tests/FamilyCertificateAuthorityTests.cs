using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FamilyHome.Server.Security;
using Microsoft.Extensions.Time.Testing;

namespace FamilyHome.Server.Tests;

public sealed class FamilyCertificateAuthorityTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));

    private static string CreateCsr(ECDsa key) =>
        new CertificateRequest("CN=ignored", key, HashAlgorithmName.SHA256).CreateSigningRequestPem();

    [Fact]
    public void IssuedDeviceCertificate_IsValid()
    {
        using var ca = FamilyCertificateAuthority.CreateNew("Muster", _time);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var deviceId = Guid.CreateVersion7();

        using var cert = ca.IssueDeviceCertificate(CreateCsr(key), deviceId);

        Assert.True(ca.Validate(cert));
        Assert.Equal($"CN={deviceId:D}", cert.Subject);
        Assert.Equal(_time.GetUtcNow().Add(FamilyCertificateAuthority.DeviceCertificateLifetime).UtcDateTime, cert.NotAfter.ToUniversalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CertificateFromOtherCa_IsRejected()
    {
        using var ca = FamilyCertificateAuthority.CreateNew("Muster", _time);
        using var otherCa = FamilyCertificateAuthority.CreateNew("Fremd", _time);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        using var foreign = otherCa.IssueDeviceCertificate(CreateCsr(key), Guid.CreateVersion7());

        Assert.False(ca.Validate(foreign));
    }

    [Fact]
    public void ExpiredCertificate_IsRejected()
    {
        using var ca = FamilyCertificateAuthority.CreateNew("Muster", _time);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var cert = ca.IssueDeviceCertificate(CreateCsr(key), Guid.CreateVersion7());

        _time.Advance(FamilyCertificateAuthority.DeviceCertificateLifetime + TimeSpan.FromMinutes(1));

        Assert.False(ca.Validate(cert));
    }

    [Fact]
    public void RsaCsr_IsRejected()
    {
        using var ca = FamilyCertificateAuthority.CreateNew("Muster", _time);
        using var rsa = RSA.Create(2048);
        var csr = new CertificateRequest("CN=x", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).CreateSigningRequestPem();

        Assert.ThrowsAny<CryptographicException>(() => ca.IssueDeviceCertificate(csr, Guid.CreateVersion7()));
    }

    [Fact]
    public void CaRoundTrip_KeepsSigningAbility()
    {
        using var ca = FamilyCertificateAuthority.CreateNew("Muster", _time);
        using var loaded = FamilyCertificateAuthority.Load(ca.CertificatePem, ca.ExportPrivateKeyPem(), _time);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        using var cert = loaded.IssueDeviceCertificate(CreateCsr(key), Guid.CreateVersion7());

        Assert.True(ca.Validate(cert));
    }
}
