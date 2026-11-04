using HortaComunitaria.Api.Features.Cultivation.ConfigureCultivarVisualGrowth;
using HortaComunitaria.Api.Features.Cultivation.ConfigureSpeciesVisualGrowth;
using HortaComunitaria.Api.Features.Cultivation.CreateSpecies;
using HortaComunitaria.Api.Features.Cultivation.GetSpeciesImage;
using HortaComunitaria.Api.Features.Cultivation.InactivateCultivar;
using HortaComunitaria.Api.Features.Cultivation.ListCultivars;
using HortaComunitaria.Api.Features.Cultivation.InactivateSpecies;
using HortaComunitaria.Api.Features.Cultivation.GetCultivar;
using HortaComunitaria.Api.Features.Cultivation.UpdateSpecies;
using HortaComunitaria.Api.Features.Cultivation.ListSpeciesHistory;
using HortaComunitaria.Api.Features.Cultivation.ListSpecies;
using HortaComunitaria.Api.Features.Cultivation.ListCultivarHistory;
using HortaComunitaria.Api.Features.Cultivation.GetSpecies;
using HortaComunitaria.Api.Features.Cultivation.CreateCultivar;
using HortaComunitaria.Api.Features.Cultivation.UpdateCultivar;
namespace HortaComunitaria.Api.Features.Cultivation;

internal static class CultivationGlobalEndpointRegistration
{
    internal static RouteGroupBuilder MapCultivationGlobalEndpoints(this RouteGroupBuilder group)
    {
        group.MapCreateSpeciesEndpoints();
        group.MapUpdateSpeciesEndpoints();
        group.MapInactivateSpeciesEndpoints();
        group.MapGetSpeciesEndpoints();
        group.MapGetSpeciesImageEndpoints();
        group.MapListSpeciesEndpoints();
        group.MapListSpeciesHistoryEndpoints();
        group.MapCreateCultivarEndpoints();
        group.MapUpdateCultivarEndpoints();
        group.MapInactivateCultivarEndpoints();
        group.MapGetCultivarEndpoints();
        group.MapListCultivarsEndpoints();
        group.MapListCultivarHistoryEndpoints();
        group.MapConfigureSpeciesVisualGrowthEndpoints();
        group.MapConfigureCultivarVisualGrowthEndpoints();
        return group;
    }
}
