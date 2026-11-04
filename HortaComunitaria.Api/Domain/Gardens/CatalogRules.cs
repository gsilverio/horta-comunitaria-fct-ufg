using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Domain.Gardens;

internal static class CatalogRules
{
    internal static Error? Validate(Guid? gardenId, string? code, string? name)
    {
        if (gardenId == Guid.Empty)
            return Error.Validation("gardenId", "Identificador da horta é obrigatório.");
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 40)
            return Error.Validation("code", "Código deve conter de 1 a 40 caracteres.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
            return Error.Validation("name", "Nome deve conter de 1 a 120 caracteres.");
        return null;
    }
}
