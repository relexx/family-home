using FamilyHome.Server.Auth;
using FamilyHome.Shared.Api;
using FamilyHome.Shared.Sync;

namespace FamilyHome.Server.Sync;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiVersions.Prefix + "/sync").RequireAuthorization();

        group.MapGet("/pull", async (long since, int? limit, HttpContext http, SyncService sync, CancellationToken ct) =>
            Results.Ok(await sync.PullAsync(http.User.GetActor(), since, limit ?? SyncService.MaxPullLimit, ct)));

        group.MapPost("/push", async (PushRequest request, HttpContext http, SyncService sync, CancellationToken ct) =>
            Results.Ok(await sync.PushAsync(http.User.GetActor(), request, ct)));

        return app;
    }
}
