using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal static class DryingLotForm
{
    internal static Result<DryingLotValues> Create(string code, string? method, DateOnly startedOn, DateOnly? endedOn, decimal? totalDryMassKg, Guid? responsibleParticipantId, DryingItemRequest[]? items)
    {
        if (items is null || items.Length > 1000) return Result<DryingLotValues>.Failure(Error.Validation("items", "Informe lista com até 1000 itens."));
        var itemsValues = new List<DryingItemValues>();
        foreach (var item in items)
        {
            if (item is null) return Result<DryingLotValues>.Failure(Error.Validation("items", "Item nulo não é permitido."));
            var created = DryingItemValues.Create(item.HarvestId, item.AllocatedMassKg);
            if (!created.IsSuccess) return Result<DryingLotValues>.Failure(created.Error);
            itemsValues.Add(created.Value);
        }

        return DryingLotValues.Create(code, method, startedOn, endedOn, totalDryMassKg, responsibleParticipantId, itemsValues);
    }
}
