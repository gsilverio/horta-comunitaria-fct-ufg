using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Domain.Costs;

namespace HortaComunitaria.Api.Domain.Activities;
/// <summary>Valores imutáveis da associação ActivityInstitution.</summary>
public sealed record ActivityInstitutionValues
{
    private ActivityInstitutionValues() { }
    /// <summary>Campo institutionId validado.</summary>
    public Guid InstitutionId { get; private init; }
    /// <summary>Campo role validado.</summary>
    public string? Role { get; private init; }
    /// <summary>Valida referências, textos e grandezas antes de construir a associação.</summary>
    public static Result<ActivityInstitutionValues> Create(Guid institutionId, string? role)
    {
        var error = (institutionId == Guid.Empty ? Error.Validation("institutionId", "Identificador inválido.") : null) ?? RecordRules.Text(role, "role", 120, false);
        if (error is not null) return Result<ActivityInstitutionValues>.Failure(error);

        return Result<ActivityInstitutionValues>.Success(new() { InstitutionId = institutionId, Role = RecordRules.Optional(role) });
    }
}
