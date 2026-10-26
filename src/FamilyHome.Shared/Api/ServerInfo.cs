namespace FamilyHome.Shared.Api;

/// <summary>
/// Versions- und Fähigkeitsaushandlung zwischen App und selbst gehostetem Server (docs/SICHERHEIT.md Kap. 5.1).
/// </summary>
public sealed record ServerInfo(string ServerVersion, int ApiVersion, string MinAppVersion, IReadOnlyList<string> Capabilities);

public static class ApiVersions
{
    public const int Current = 1;
    public const string Prefix = "/api/v1";
}
