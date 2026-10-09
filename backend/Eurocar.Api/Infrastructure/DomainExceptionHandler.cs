using System.Text.Json;
using Eurocar.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Eurocar.Api.Infrastructure;

/// <summary>Turns DomainException into ProblemDetails { status, title, code }.</summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is not DomainException d) return false;
        var problem = new ProblemDetails { Status = d.Status, Title = d.Message, Extensions = { ["code"] = d.Code } };
        ctx.Response.StatusCode = d.Status;
        await ctx.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", ct);
        return true;
    }
}
