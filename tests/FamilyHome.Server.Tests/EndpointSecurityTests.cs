using System.Net;
using FamilyHome.Server.Api;
using FamilyHome.Server.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyHome.Server.Tests;

/// <summary>Security Gate (docs/SICHERHEIT.md Kap. 7.2): Endpunkt-Inventar und Schutz vor gefälschten Zertifikat-Headern.</summary>
public sealed class EndpointSecurityTests(TestServerFactory factory) : IClassFixture<TestServerFactory>
{
    [Fact]
    public void EveryEndpoint_RequiresAuthorization_ExceptAllowlist()
    {
        _ = factory.CreateClient();
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
        Assert.NotEmpty(endpoints);

        var violations = new List<string>();
        foreach (var endpoint in endpoints)
        {
            var route = "/" + endpoint.RoutePattern.RawText?.TrimStart('/');
            var isPublic = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
                || endpoint.Metadata.GetMetadata<IAuthorizeData>() is null;

            if (isPublic && !DeviceEndpoints.PublicRoutes.Contains(route))
                violations.Add(route);
        }

        Assert.True(violations.Count == 0, "Endpunkte ohne Autorisierung, die nicht auf der Allowlist stehen: " + string.Join(", ", violations));
    }

    [Fact]
    public void Allowlist_ContainsOnlyExistingRoutes()
    {
        _ = factory.CreateClient();
        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Select(e => "/" + e.RoutePattern.RawText?.TrimStart('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(DeviceEndpoints.PublicRoutes, r => Assert.Contains(r, routes));
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutCertificate_IsRejected()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/api/v1/info", UriKind.Relative), TestContext.Current.CancellationToken);
        AssertRejected(response);
    }

    [Fact]
    public async Task ForgedCertificateHeader_FromUntrustedAddress_IsIgnored()
    {
        // Ein von der Familien-CA korrekt ausgestelltes Zertifikat – aber der Header kommt nicht vom Proxy.
        using var key = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var csr = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=x", key, System.Security.Cryptography.HashAlgorithmName.SHA256).CreateSigningRequestPem();
        using var cert = factory.Ca.IssueDeviceCertificate(csr, Guid.CreateVersion7());

        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/info");
        request.Headers.Add(DeviceAuthentication.ClientCertificateHeader, Convert.ToBase64String(cert.RawData));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        AssertRejected(response);
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/api/v1/info", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task ValidDeviceCertificate_FromTrustedProxy_IsAccepted()
    {
        using var cert = factory.CreateRegisteredDevice();

        var context = await SendViaProxyAsync("/api/v1/info", cert, remoteAddress: TestServerFactory.ProxyAddress);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task ValidDeviceCertificate_WithForwardedClientIp_IsStillAccepted()
    {
        // Regressionstest: UseForwardedHeaders ersetzt die Remote-Adresse – der Header-Filter muss vorher laufen.
        using var cert = factory.CreateRegisteredDevice();

        var context = await SendViaProxyAsync("/api/v1/info", cert, remoteAddress: TestServerFactory.ProxyAddress, forwardedFor: "203.0.113.9");

        Assert.Equal("203.0.113.9", context.Connection.RemoteIpAddress?.ToString());

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task RevokedDevice_IsRejected()
    {
        using var cert = factory.CreateRegisteredDevice(active: false);

        var context = await SendViaProxyAsync("/api/v1/info", cert, remoteAddress: TestServerFactory.ProxyAddress);

        Assert.Contains(context.Response.StatusCode, new[] { 401, 403 });
    }

    [Fact]
    public async Task ValidCertificateHeader_FromUntrustedAddress_IsRejected()
    {
        using var cert = factory.CreateRegisteredDevice();

        var context = await SendViaProxyAsync("/api/v1/info", cert, remoteAddress: "192.0.2.50");

        Assert.Contains(context.Response.StatusCode, new[] { 401, 403 });
    }

    [Fact]
    public async Task CertificateFromForeignCa_IsRejected()
    {
        using var foreignCa = FamilyHome.Server.Security.FamilyCertificateAuthority.CreateNew("Fremd", TimeProvider.System);
        using var key = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var csr = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=x", key, System.Security.Cryptography.HashAlgorithmName.SHA256).CreateSigningRequestPem();
        using var cert = foreignCa.IssueDeviceCertificate(csr, Guid.CreateVersion7());

        var context = await SendViaProxyAsync("/api/v1/info", cert, remoteAddress: TestServerFactory.ProxyAddress);

        Assert.Contains(context.Response.StatusCode, new[] { 401, 403 });
    }

    private Task<Microsoft.AspNetCore.Http.HttpContext> SendViaProxyAsync(
        string path, System.Security.Cryptography.X509Certificates.X509Certificate2 cert, string remoteAddress, string? forwardedFor = null)
    {
        _ = factory.CreateClient();
        return factory.Server.SendAsync(ctx =>
        {
            ctx.Request.Method = "GET";
            ctx.Request.Path = path;
            ctx.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
            ctx.Request.Headers[DeviceAuthentication.ClientCertificateHeader] = Convert.ToBase64String(cert.RawData);
            // Wie Caddy: Der Proxy terminiert TLS und meldet das per X-Forwarded-Proto. Der Zertifikat-Handler
            // authentifiziert nur HTTPS-Anfragen – ohne vertrauenswürdige Forwarded Headers gäbe es keine Anmeldung.
            ctx.Request.Headers["X-Forwarded-Proto"] = "https";
            ctx.Request.Headers["X-Forwarded-For"] = forwardedFor ?? "198.51.100.7";
        }, TestContext.Current.CancellationToken);
    }

    // Der Zertifikat-Handler antwortet bei fehlendem/ungültigem Zertifikat mit 403 (Challenge = Forbid).
    private static void AssertRejected(HttpResponseMessage response) =>
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
}
