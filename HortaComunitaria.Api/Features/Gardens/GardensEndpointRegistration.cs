using HortaComunitaria.Api.Features.Gardens.GetIrrigationSector;
using HortaComunitaria.Api.Features.Gardens.SaveLayout;
using HortaComunitaria.Api.Features.Gardens.GetLayout;
using HortaComunitaria.Api.Features.Gardens.GetGarden;
using HortaComunitaria.Api.Features.Gardens.ConfigureBed;
using HortaComunitaria.Api.Features.Gardens.UpdateGarden;
using HortaComunitaria.Api.Features.Gardens.ListBedSectorAssignments;
using HortaComunitaria.Api.Features.Gardens.ListBedsForIrrigation;
using HortaComunitaria.Api.Features.Gardens.ListIrrigationSectors;
using HortaComunitaria.Api.Features.Gardens.InactivateIrrigationSector;
using HortaComunitaria.Api.Features.Gardens.CreateIrrigationSector;
using HortaComunitaria.Api.Features.Gardens.ListBedCatalog;
using HortaComunitaria.Api.Features.Gardens.CreateBed;
using HortaComunitaria.Api.Features.Gardens.ListIrrigationSectorHistory;
using HortaComunitaria.Api.Features.Gardens.InactivateBed;
using HortaComunitaria.Api.Features.Gardens.AssignBedToSector;
using HortaComunitaria.Api.Features.Gardens.ListGardenHistory;
using HortaComunitaria.Api.Features.Gardens.GetBed;
using HortaComunitaria.Api.Features.Gardens.ListBedHistory;
using HortaComunitaria.Api.Features.Gardens.InactivateGarden;
using HortaComunitaria.Api.Features.Gardens.CreateGarden;
using HortaComunitaria.Api.Features.Gardens.UpdateIrrigationSector;
using HortaComunitaria.Api.Features.Gardens.ListBedConfigurations;
using HortaComunitaria.Api.Features.Gardens.ListGardens;
using HortaComunitaria.Api.Features.Gardens.UpdateBed;

using HortaComunitaria.Api.Features.Gardens.ListGardenCatalog;
using HortaComunitaria.Api.Features.Gardens.ListIrrigationSectorCatalog;

namespace HortaComunitaria.Api.Features.Gardens;

internal static class GardensEndpointRegistration
{
    internal static RouteGroupBuilder MapGardensEndpoints(this RouteGroupBuilder group) =>
        group.MapSaveLayoutEndpoints().MapGetLayoutEndpoints().MapListGardenCatalogEndpoints().MapListIrrigationSectorCatalogEndpoints().MapListGardensEndpoints().MapListIrrigationSectorsEndpoints().MapListBedsForIrrigationEndpoints().MapCreateGardenEndpoints().MapUpdateGardenEndpoints().MapGetGardenEndpoints().MapInactivateGardenEndpoints().MapListGardenHistoryEndpoints().MapCreateIrrigationSectorEndpoints().MapUpdateIrrigationSectorEndpoints().MapGetIrrigationSectorEndpoints().MapInactivateIrrigationSectorEndpoints().MapListIrrigationSectorHistoryEndpoints().MapCreateBedEndpoints().MapUpdateBedEndpoints().MapGetBedEndpoints().MapInactivateBedEndpoints().MapListBedHistoryEndpoints().MapConfigureBedEndpoints().MapListBedConfigurationsEndpoints().MapAssignBedToSectorEndpoints().MapListBedSectorAssignmentsEndpoints().MapListBedCatalogEndpoints();
}

