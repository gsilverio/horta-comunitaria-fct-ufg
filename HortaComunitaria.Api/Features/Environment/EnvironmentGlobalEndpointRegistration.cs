using HortaComunitaria.Api.Features.Environment.ListEnvironmentalVariableHistory;
using HortaComunitaria.Api.Features.Environment.InactivateEnvironmentalVariable;
using HortaComunitaria.Api.Features.Environment.UpdateEnvironmentalVariable;
using HortaComunitaria.Api.Features.Environment.CreateEnvironmentalVariable;
using HortaComunitaria.Api.Features.Environment.GetEnvironmentalVariable;
using HortaComunitaria.Api.Features.Environment.ListEnvironmentalVariables;
namespace HortaComunitaria.Api.Features.Environment;

internal static class EnvironmentGlobalEndpointRegistration
{
    internal static RouteGroupBuilder MapEnvironmentGlobalEndpoints(this RouteGroupBuilder group)
    {
        group.MapCreateEnvironmentalVariableEndpoints();
        group.MapUpdateEnvironmentalVariableEndpoints();
        group.MapInactivateEnvironmentalVariableEndpoints();
        group.MapGetEnvironmentalVariableEndpoints();
        group.MapListEnvironmentalVariablesEndpoints();
        group.MapListEnvironmentalVariableHistoryEndpoints();
        return group;
    }
}
