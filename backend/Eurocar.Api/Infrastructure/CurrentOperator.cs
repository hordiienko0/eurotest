using Eurocar.Api.Domain;

namespace Eurocar.Api.Infrastructure;

/// <summary>Who is acting. Today: X-Operator-Id header; later: Entra ID claims.</summary>
public interface ICurrentOperator
{
    int RequireId();
}

public sealed class HeaderCurrentOperator(IHttpContextAccessor http) : ICurrentOperator
{
    public int RequireId() =>
        int.TryParse(http.HttpContext?.Request.Headers["X-Operator-Id"], out var id) ? id : throw Errors.OperatorRequired();
}
