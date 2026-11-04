using HortaComunitaria.Api.Features.Costs.ListFinancialEntries;
using HortaComunitaria.Api.Features.Costs.RegisterFinancialEntry;
using HortaComunitaria.Api.Features.Costs.GetFinancialEntry;
using HortaComunitaria.Api.Features.Costs.ListFinancialEntryHistory;
using HortaComunitaria.Api.Features.Costs.CorrectFinancialEntry;
using HortaComunitaria.Api.Features.Costs.InvalidateFinancialEntry;
namespace HortaComunitaria.Api.Features.Costs;

internal static class CostsEndpointRegistration
{
    internal static RouteGroupBuilder MapCostsEndpoints(this RouteGroupBuilder group)
    {
        group.MapRegisterFinancialEntryEndpoints();
        group.MapCorrectFinancialEntryEndpoints();
        group.MapInvalidateFinancialEntryEndpoints();
        group.MapGetFinancialEntryEndpoints();
        group.MapListFinancialEntriesEndpoints();
        group.MapListFinancialEntryHistoryEndpoints();
        return group;
    }
}
