using System.Reflection;
using FamilyHome.Server.Data;
using FamilyHome.Server.Security;
using FamilyHome.Shared.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyHome.Server.Api;

public sealed record SetupRequest(string SetupCode, string FamilyName, string DisplayName);
public sealed record JoinRequest(string InvitationCode, string DeviceName, string CsrPem);
public sealed record RenewRequest(string CsrPem);

public static class DeviceEndpoints
{
    /// <summary>
    /// Endpunkte ohne Gerätezertifikat. Jede Erweiterung dieser Liste ist sicherheitsrelevant
    /// und wird vom Endpunkt-Inventar-Test erzwungen (docs/SICHERHEIT.md Kap. 7.2).
    /// </summary>
    public static readonly IReadOnlySet<string> PublicRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "/.well-known/assetlinks.json",
        ApiVersions.Prefix + "/setup",
        ApiVersions.Prefix + "/join",
        "/health", // nur im internen Container-Netz, wird vom Proxy nicht weitergeleitet
    };

    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/assetlinks.json", (IOptions<SecurityOptions> options) => Results.Json(new[]
        {
            new
            {
                relation = new[] { "delegate_permission/common.get_login_creds" },
                target = new
                {
                    @namespace = "android_app",
                    package_name = options.Value.AppPackageName,
                    sha256_cert_fingerprints = options.Value.AppSigningCertFingerprints,
                },
            },
        })).AllowAnonymous();

        var open = app.MapGroup(ApiVersions.Prefix).AllowAnonymous().RequireRateLimiting(SecurityExtensions.AuthRateLimitPolicy);

        // TODO Phase 0: Setup-Code prüfen (konstante Laufzeit), ersten Eltern-Account + Passkey anlegen,
        // Wiederherstellungscode ausgeben, danach Endpunkt dauerhaft deaktivieren.
        open.MapPost("/setup", (SetupRequest _) => Results.StatusCode(StatusCodes.Status501NotImplemented));

        // Nur erreichbar, solange eine Einladung offen ist (verkleinert die öffentliche Angriffsfläche).
        open.MapPost("/join", async (JoinRequest _, FamilyHomeDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            var now = time.GetUtcNow();
            if (!await db.Invitations.AnyAsync(i => i.RedeemedAt == null && i.ExpiresAt > now, ct))
                return Results.NotFound();

            // TODO Phase 0: Code-Hash prüfen, Passkey registrieren (falls für die Rolle verlangt), Attestation prüfen,
            // Gerät anlegen (inaktiv bis Eltern bestätigen), Zertifikat aus der CSR ausstellen, Audit-Log schreiben.
            return Results.StatusCode(StatusCodes.Status501NotImplemented);
        });

        var device = app.MapGroup(ApiVersions.Prefix).RequireAuthorization();

        device.MapGet("/info", (IOptions<SecurityOptions> options) => Results.Ok(new ServerInfo(
            typeof(DeviceEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0",
            ApiVersions.Current,
            options.Value.MinAppVersion,
            ["sync.v1"])));

        // TODO Phase 0: neues Zertifikat aus CSR ausstellen (gleicher Schlüssel oder Schlüsselwechsel), Thumbprint im Device ersetzen.
        device.MapPost("/device/renew", (RenewRequest _) => Results.StatusCode(StatusCodes.Status501NotImplemented));

        return app;
    }
}
