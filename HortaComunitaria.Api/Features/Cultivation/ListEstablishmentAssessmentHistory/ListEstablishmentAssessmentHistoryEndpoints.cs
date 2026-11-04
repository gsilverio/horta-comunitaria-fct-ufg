using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListEstablishmentAssessmentHistory;

internal static class ListEstablishmentAssessmentHistoryEndpoints
{
    internal static RouteGroupBuilder MapListEstablishmentAssessmentHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/establishment-assessments/{id:guid}/history", async (Guid gardenId, Guid id, ListEstablishmentAssessmentHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(gardenId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListEstablishmentAssessmentHistory").WithTags("Cultivo e produção")
            .WithSummary("Listar histórico de avaliação de pegamento").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<EstablishmentAssessmentHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
