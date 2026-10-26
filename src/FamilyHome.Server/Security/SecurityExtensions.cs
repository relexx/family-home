using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace FamilyHome.Server.Security;

/// <summary>Server-Härtung, siehe docs/SICHERHEIT.md Kap. 2 und 3.</summary>
public static class SecurityExtensions
{
    public const string AuthRateLimitPolicy = "auth";

    public static WebApplicationBuilder AddFamilyHomeSecurity(this WebApplicationBuilder builder)
    {
        ValidateSecrets(builder);
        builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.Section));

        builder.WebHost.ConfigureKestrel(o =>
        {
            o.AddServerHeader = false;
            o.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // Fotos/Anhänge laufen später über einen eigenen Endpunkt
        });

        // Echte Client-IP (für Rate Limits und Audit-Log) nur aus X-Forwarded-For des eigenen Proxys übernehmen
        var trustedProxies = builder.Configuration.GetSection(SecurityOptions.Section).Get<SecurityOptions>()?.TrustedProxyNetworks ?? [];
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.ForwardLimit = 1;
            o.KnownProxies.Clear();
            o.KnownIPNetworks.Clear();
            foreach (var network in trustedProxies)
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        });

        builder.Services.AddHsts(o =>
        {
            o.MaxAge = TimeSpan.FromDays(365);
            o.IncludeSubDomains = true;
        });

        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Auth-Endpunkte: 10 Versuche pro Minute und IP
            o.AddPolicy(AuthRateLimitPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
            // Alle übrigen Anfragen: großzügiges globales Limit pro IP
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx => RateLimitPartition.GetTokenBucketLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                }));
        });

        return builder;
    }

    public static WebApplication UseFamilyHomeSecurity(this WebApplication app)
    {
        app.UseForwardedHeaders();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (ctx, next) =>
        {
            var h = ctx.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["Referrer-Policy"] = "no-referrer";
            h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            h["X-Frame-Options"] = "DENY";
            h["Cross-Origin-Resource-Policy"] = "same-origin";
            h["Cache-Control"] = "no-store";
            await next();
        });

        app.UseRateLimiter();
        return app;
    }

    /// <summary>Startet außerhalb der Entwicklung nicht mit schwachen oder fehlenden Geheimnissen.</summary>
    private static void ValidateSecrets(WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment()) return;

        var cs = builder.Configuration.GetConnectionString("FamilyHome") ?? "";
        if (cs.Contains("Password=familyhome", StringComparison.OrdinalIgnoreCase) || cs.Contains("change-me", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Datenbank-Passwort ist ein Standardwert.");

        var security = builder.Configuration.GetSection(SecurityOptions.Section).Get<SecurityOptions>() ?? new SecurityOptions();
        if (security.TrustedProxyNetworks.Length == 0)
            throw new InvalidOperationException("Security:TrustedProxyNetworks fehlt – ohne vertrauenswürdigen Proxy ist keine Geräte-Authentifizierung möglich.");
    }
}
