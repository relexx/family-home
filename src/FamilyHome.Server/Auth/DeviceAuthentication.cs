using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FamilyHome.Server.Data;
using FamilyHome.Server.Security;
using FamilyHome.Shared.Model;
using FamilyHome.Shared.Security;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyHome.Server.Auth;

public static class FamilyHomeClaims
{
    public const string AccountId = "fh_account";
    public const string PersonId = "fh_person";
    public const string DeviceId = "fh_device";
}

public static class ClaimsPrincipalExtensions
{
    public static Actor GetActor(this ClaimsPrincipal user) => new(
        Guid.Parse(user.FindFirstValue(FamilyHomeClaims.AccountId)!),
        Guid.Parse(user.FindFirstValue(FamilyHomeClaims.PersonId)!),
        Enum.Parse<Role>(user.FindFirstValue(ClaimTypes.Role)!));

    public static Guid GetDeviceId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(FamilyHomeClaims.DeviceId)!);
}

/// <summary>
/// Geräte-Authentifizierung per Client-Zertifikat (docs/SICHERHEIT.md Kap. 2.1 und 3.1):
/// Caddy prüft mTLS und reicht das Zertifikat im Header weiter; der Server prüft es erneut
/// (Familien-CA, Gültigkeit, Verwendungszweck) und gleicht es mit der Gerätetabelle ab.
/// </summary>
public static class DeviceAuthentication
{
    /// <summary>Von Caddy gesetzt: DER-Zertifikat Base64-kodiert (<c>{http.request.tls.client.certificate_der_base64}</c>).</summary>
    public const string ClientCertificateHeader = "X-Client-Cert";

    public static WebApplicationBuilder AddDeviceAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddCertificateForwarding(o =>
        {
            o.CertificateHeader = ClientCertificateHeader;
            o.HeaderConverter = value => X509CertificateLoader.LoadCertificate(Convert.FromBase64String(value));
        });

        builder.Services
            .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(o =>
            {
                o.AllowedCertificateTypes = CertificateTypes.Chained;
                o.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
                o.RevocationMode = X509RevocationMode.NoCheck; // Sperrung über die Gerätetabelle
                o.ValidateCertificateUse = true;
                o.ValidateValidityPeriod = true;
                o.Events = new CertificateAuthenticationEvents { OnCertificateValidated = OnCertificateValidatedAsync };
            });

        // CA erst nach CertificateAuthorityProvider.InitializeAsync verfügbar → Optionen spät setzen
        builder.Services.AddSingleton<IPostConfigureOptions<CertificateAuthenticationOptions>, TrustStoreConfigurator>();
        builder.Services.AddAuthorization();
        return builder;
    }

    /// <summary>
    /// Muss als ERSTE Middleware laufen – vor UseForwardedHeaders, das die Remote-Adresse durch die Client-IP ersetzt.
    /// </summary>
    public static WebApplication UseTrustedProxyCertificateHeaderFilter(this WebApplication app)
    {
        app.UseMiddleware<UntrustedCertificateHeaderFilter>();
        return app;
    }

    public static WebApplication UseDeviceAuthentication(this WebApplication app)
    {
        app.UseCertificateForwarding();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    private static async Task OnCertificateValidatedAsync(CertificateValidatedContext ctx)
    {
        var services = ctx.HttpContext.RequestServices;
        var ca = services.GetRequiredService<CertificateAuthorityProvider>().Current;
        if (!ca.Validate(ctx.ClientCertificate))
        {
            ctx.Fail("Zertifikat stammt nicht von der Familien-CA.");
            return;
        }

        var thumbprint = ctx.ClientCertificate.GetCertHashString(HashAlgorithmName.SHA256);
        var db = services.GetRequiredService<FamilyHomeDbContext>();
        var device = await db.Devices.AsNoTracking()
            .Where(d => d.CertThumbprint == thumbprint && d.IsActive)
            .Join(db.Accounts.Where(a => a.IsActive), d => d.AccountId, a => a.Id, (d, a) => new { Device = d, Account = a })
            .SingleOrDefaultAsync(ctx.HttpContext.RequestAborted);

        if (device is null)
        {
            ctx.Fail("Gerät unbekannt oder gesperrt.");
            return;
        }

        ctx.Principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(FamilyHomeClaims.AccountId, device.Account.Id.ToString()),
            new Claim(FamilyHomeClaims.PersonId, device.Account.PersonId.ToString()),
            new Claim(FamilyHomeClaims.DeviceId, device.Device.Id.ToString()),
            new Claim(ClaimTypes.Role, device.Account.Role.ToString()),
        ], ctx.Scheme.Name));
        ctx.Success();
    }

    private sealed class TrustStoreConfigurator(CertificateAuthorityProvider ca) : IPostConfigureOptions<CertificateAuthenticationOptions>
    {
        public void PostConfigure(string? name, CertificateAuthenticationOptions options) =>
            options.CustomTrustStore.Add(ca.Current.Certificate);
    }
}

/// <summary>
/// Verwirft den Zertifikat-Header, wenn die Anfrage nicht aus einem vertrauenswürdigen Proxy-Netz kommt.
/// Verhindert, dass jemand mit direktem Zugriff auf den Server-Port ein Zertifikat „behauptet“.
/// </summary>
public sealed class UntrustedCertificateHeaderFilter(RequestDelegate next, IOptions<SecurityOptions> options, IHostEnvironment env, ILogger<UntrustedCertificateHeaderFilter> logger)
{
    private readonly IPNetwork[] _trusted = [.. options.Value.TrustedProxyNetworks.Select(IPNetwork.Parse)];

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(DeviceAuthentication.ClientCertificateHeader) && !IsTrusted(context.Connection.RemoteIpAddress))
        {
            logger.LogWarning("Zertifikat-Header von nicht vertrauenswürdiger Adresse {Remote} verworfen", context.Connection.RemoteIpAddress);
            context.Request.Headers.Remove(DeviceAuthentication.ClientCertificateHeader);
        }

        return next(context);
    }

    private bool IsTrusted(IPAddress? remote)
    {
        if (remote is null) return false;
        if (remote.IsIPv4MappedToIPv6) remote = remote.MapToIPv4();
        if (env.IsDevelopment() && IPAddress.IsLoopback(remote)) return true;
        return _trusted.Any(n => n.Contains(remote));
    }
}
