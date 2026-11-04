using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.ListActivities;

internal static class ListActivitiesEndpoints
{
    internal static RouteGroupBuilder MapListActivitiesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/activities", async (Guid gardenId, ListActivitiesQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, Guid? bedId = null, Guid? sectorId = null, Guid? cycleId = null, Guid? occurrenceId = null, string? category = null, bool? isSocial = null, bool? isEducational = null, Guid? institutionId = null) => (await handler.HandleAsync(gardenId, new ListActivitiesRequest(from, to, page, pageSize, validity, bedId, sectorId, cycleId, occurrenceId, category, isSocial, isEducational, institutionId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Activities.ListActivities").WithTags("Atividades")
            .WithSummary("Listar Activities").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<ActivityResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
