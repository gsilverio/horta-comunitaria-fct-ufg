using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Common.Pagination;

/// <summary>Limites únicos de paginação; rejeita offsets fora da capacidade do contrato.</summary>
public sealed record PaginationRequest(int Page = 1, int PageSize = PaginationRequest.DefaultPageSize)
{
    /// <summary>Tamanho padrão das consultas.</summary>
    public const int DefaultPageSize = 20;
    /// <summary>Tamanho máximo para limitar a materialização.</summary>
    public const int MaximumPageSize = 100;
    /// <summary>Offset válido após validação.</summary>
    public int Skip => checked((Page - 1) * PageSize);
    /// <summary>Valida limites antes de consultar o banco.</summary>
    public Result<Unit> Validate()
    {
        if (Page < 1 || (long)(Page - 1) * PageSize > int.MaxValue)
            return Result<Unit>.Failure(Error.Validation("page", "Página deve ser positiva e possuir offset válido."));
        if (PageSize is < 1 or > MaximumPageSize)
            return Result<Unit>.Failure(Error.Validation("pageSize", "Tamanho da página deve estar entre 1 e 100."));
        return Result<Unit>.Success(new Unit());
    }
}
