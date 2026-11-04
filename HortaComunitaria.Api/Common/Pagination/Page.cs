using System.Text.Json.Serialization;

namespace HortaComunitaria.Api.Common.Pagination;

/// <summary>Página com contagem do recorte completo, independente dos itens materializados.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, [property: JsonPropertyName("page")] int PageNumber, int PageSize, int TotalCount)
{
    /// <summary>Quantidade de páginas do recorte.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    /// <summary>Indica se há página anterior à solicitada.</summary>
    public bool HasPreviousPage => PageNumber > 1;
    /// <summary>Indica se há página seguinte no recorte.</summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
