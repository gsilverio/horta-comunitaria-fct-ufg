using HortaComunitaria.Api.Features.Activities.GetInput;
using HortaComunitaria.Api.Features.Activities.ListInputs;
using HortaComunitaria.Api.Features.Activities.InactivateInput;
using HortaComunitaria.Api.Features.Activities.CreateInstitution;
using HortaComunitaria.Api.Features.Activities.ListInstitutions;
using HortaComunitaria.Api.Features.Activities.UpdateInstitution;
using HortaComunitaria.Api.Features.Activities.ListInputHistory;
using HortaComunitaria.Api.Features.Activities.UpdateInput;
using HortaComunitaria.Api.Features.Activities.ListInstitutionHistory;
using HortaComunitaria.Api.Features.Activities.InactivateInstitution;
using HortaComunitaria.Api.Features.Activities.CreateInput;
using HortaComunitaria.Api.Features.Activities.GetInstitution;
namespace HortaComunitaria.Api.Features.Activities;

internal static class ActivitiesGlobalEndpointRegistration
{
    internal static RouteGroupBuilder MapActivitiesGlobalEndpoints(this RouteGroupBuilder group)
    {
        group.MapCreateInputEndpoints();
        group.MapUpdateInputEndpoints();
        group.MapInactivateInputEndpoints();
        group.MapGetInputEndpoints();
        group.MapListInputsEndpoints();
        group.MapListInputHistoryEndpoints();
        group.MapCreateInstitutionEndpoints();
        group.MapUpdateInstitutionEndpoints();
        group.MapInactivateInstitutionEndpoints();
        group.MapGetInstitutionEndpoints();
        group.MapListInstitutionsEndpoints();
        group.MapListInstitutionHistoryEndpoints();
        return group;
    }
}
