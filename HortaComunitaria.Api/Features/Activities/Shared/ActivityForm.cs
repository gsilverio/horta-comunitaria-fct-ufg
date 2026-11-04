using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.Shared;

internal static class ActivityForm
{
    internal static Result<ActivityValues> Create(string title, string category, DateOnly performedOn, string? description, Guid? bedId, Guid? sectorId, Guid? cycleId, Guid? occurrenceId, bool isOperational, bool isSocial, bool isEducational, string? objective, string? modality, int? reportedAudienceTotal, bool? audienceListComplete, ActivityParticipantRequest[]? participants, InputUsageRequest[]? inputUsages, ActivityInstitutionRequest[]? institutions)
    {
        if (participants is null || participants.Length > 1000) return Result<ActivityValues>.Failure(Error.Validation("participants", "Informe lista com até 1000 itens."));
        var participantsValues = new List<ActivityParticipantValues>();
        foreach (var item in participants)
        {
            if (item is null) return Result<ActivityValues>.Failure(Error.Validation("participants", "Item nulo não é permitido."));
            var created = ActivityParticipantValues.Create(item.ParticipantId, item.Role, item.WorkHours, item.CountsAsAudience);
            if (!created.IsSuccess) return Result<ActivityValues>.Failure(created.Error);
            participantsValues.Add(created.Value);
        }
        if (inputUsages is null || inputUsages.Length > 1000) return Result<ActivityValues>.Failure(Error.Validation("inputUsages", "Informe lista com até 1000 itens."));
        var inputUsagesValues = new List<InputUsageValues>();
        foreach (var item in inputUsages)
        {
            if (item is null) return Result<ActivityValues>.Failure(Error.Validation("inputUsages", "Item nulo não é permitido."));
            var created = InputUsageValues.Create(item.InputId, item.Quantity, item.Unit);
            if (!created.IsSuccess) return Result<ActivityValues>.Failure(created.Error);
            inputUsagesValues.Add(created.Value);
        }
        if (institutions is null || institutions.Length > 1000) return Result<ActivityValues>.Failure(Error.Validation("institutions", "Informe lista com até 1000 itens."));
        var institutionsValues = new List<ActivityInstitutionValues>();
        foreach (var item in institutions)
        {
            if (item is null) return Result<ActivityValues>.Failure(Error.Validation("institutions", "Item nulo não é permitido."));
            var created = ActivityInstitutionValues.Create(item.InstitutionId, item.Role);
            if (!created.IsSuccess) return Result<ActivityValues>.Failure(created.Error);
            institutionsValues.Add(created.Value);
        }

        return ActivityValues.Create(title, category, performedOn, description, bedId, sectorId, cycleId, occurrenceId, isOperational, isSocial, isEducational, objective, modality, reportedAudienceTotal, audienceListComplete, participantsValues, inputUsagesValues, institutionsValues);
    }
}
