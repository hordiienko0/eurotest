using Eurocar.Api.Infrastructure;

namespace Eurocar.Api.Features.Moves;

public static class MovesEndpoints
{
    public static void MapMoves(this RouteGroupBuilder api)
    {
        api.MapGet("/moves", (MoveService s) => s.JournalAsync());
        api.MapGet("/moves/active", async (MoveService s, ICurrentOperator op) =>
            await s.ActiveAsync(op.RequireId()) is { } m ? Results.Ok(m) : Results.NoContent());
        api.MapPost("/moves", (StartMoveDto d, MoveService s, ICurrentOperator op) => s.StartAsync(d.CarId, op.RequireId()));
        api.MapPut("/moves/{id:int}/target", (int id, SetTargetDto d, MoveService s, ICurrentOperator op) =>
            s.SetTargetAsync(id, op.RequireId(), d));
        api.MapPost("/moves/{id:int}/photo", async (int id, IFormFile file, MoveService s, ICurrentOperator op) =>
        {
            await using var stream = file.OpenReadStream();
            return await s.AttachPhotoAsync(id, op.RequireId(), stream, file.ContentType, file.Length);
        }).DisableAntiforgery();
        api.MapPost("/moves/{id:int}/complete", (int id, MoveService s, ICurrentOperator op) => s.CompleteAsync(id, op.RequireId()));
        api.MapPost("/moves/{id:int}/cancel", (int id, CancelMoveDto? d, MoveService s, ICurrentOperator op) =>
            s.CancelAsync(id, op.RequireId(), d?.FallbackSpot));
    }
}
