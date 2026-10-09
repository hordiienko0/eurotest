namespace Eurocar.Api.Features.Zones;

public static class ZonesEndpoints
{
    public static void MapZones(this RouteGroupBuilder api)
    {
        api.MapGet("/zones", (ZoneService s) => s.OverviewAsync());
        api.MapGet("/zones/{code}", (string code, ZoneService s) => s.DetailAsync(code));
        api.MapPost("/zones", (ZoneUpsertDto d, ZoneService s) => s.CreateAsync(d));
        api.MapPut("/zones/{code}", (string code, ZoneUpsertDto d, ZoneService s) => s.UpdateAsync(code, d));
        api.MapDelete("/zones/{code}", async (string code, ZoneService s) =>
        {
            await s.DeleteAsync(code);
            return Results.NoContent();
        });
    }
}
