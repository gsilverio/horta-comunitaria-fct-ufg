using HortaComunitaria.Api.Features.Environment.RegisterEnvironmentalMeasurement;
using HortaComunitaria.Api.Features.Environment.CorrectEnvironmentalMeasurement;
using HortaComunitaria.Api.Features.Environment.ListEnvironmentalMeasurementHistory;
using HortaComunitaria.Api.Features.Environment.ListSamplingPointHistory;
using HortaComunitaria.Api.Features.Environment.GetSamplingPoint;
using HortaComunitaria.Api.Features.Environment.InactivateSamplingPoint;
using HortaComunitaria.Api.Features.Environment.GetEnvironmentalMeasurement;
using HortaComunitaria.Api.Features.Environment.ListEnvironmentalMeasurements;
using HortaComunitaria.Api.Features.Environment.ListSamplingPoints;
using HortaComunitaria.Api.Features.Environment.UpdateSamplingPoint;
using HortaComunitaria.Api.Features.Environment.CreateSamplingPoint;
using HortaComunitaria.Api.Features.Environment.InvalidateEnvironmentalMeasurement;
namespace HortaComunitaria.Api.Features.Environment;

internal static class EnvironmentEndpointRegistration
{
    internal static RouteGroupBuilder MapEnvironmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapCreateSamplingPointEndpoints();
        group.MapUpdateSamplingPointEndpoints();
        group.MapInactivateSamplingPointEndpoints();
        group.MapGetSamplingPointEndpoints();
        group.MapListSamplingPointsEndpoints();
        group.MapListSamplingPointHistoryEndpoints();
        group.MapRegisterEnvironmentalMeasurementEndpoints();
        group.MapCorrectEnvironmentalMeasurementEndpoints();
        group.MapInvalidateEnvironmentalMeasurementEndpoints();
        group.MapGetEnvironmentalMeasurementEndpoints();
        group.MapListEnvironmentalMeasurementsEndpoints();
        group.MapListEnvironmentalMeasurementHistoryEndpoints();
        return group;
    }
}
