using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListEstablishmentAssessments;

internal static class ListEstablishmentAssessmentsEndpoints
{
    internal static RouteGroupBuilder MapListEstablishmentAssessmentsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/establishment-assessments", async (Guid gardenId, ListEstablishmentAssessmentsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, Guid? plantingId = null, string? method = null, Guid? cycleId = null, Guid? speciesId = null) => (await handler.HandleAsync(gardenId, new ListEstablishmentAssessmentsRequest(from, to, page, pageSize, validity, plantingId, method, cycleId, speciesId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Cultivation.ListEstablishmentAssessments").WithTags("Cultivo e produção")
            .WithSummary("Listar avaliação de pegamento").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<EstablishmentAssessmentResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
