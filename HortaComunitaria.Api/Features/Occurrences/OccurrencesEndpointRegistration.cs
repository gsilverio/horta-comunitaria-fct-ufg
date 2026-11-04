using HortaComunitaria.Api.Features.Occurrences.InvalidateOccurrence;
using HortaComunitaria.Api.Features.Occurrences.CorrectOccurrence;
using HortaComunitaria.Api.Features.Occurrences.ListOccurrenceHistory;
using HortaComunitaria.Api.Features.Occurrences.OpenOccurrence;
using HortaComunitaria.Api.Features.Occurrences.ResolveOccurrence;
using HortaComunitaria.Api.Features.Occurrences.ListOccurrences;
using HortaComunitaria.Api.Features.Occurrences.ChangeOccurrenceStatus;
using HortaComunitaria.Api.Features.Occurrences.GetOccurrence;
namespace HortaComunitaria.Api.Features.Occurrences;

internal static class OccurrencesEndpointRegistration
{
    internal static RouteGroupBuilder MapOccurrencesEndpoints(this RouteGroupBuilder group)
    {
        group.MapOpenOccurrenceEndpoints();
        group.MapCorrectOccurrenceEndpoints();
        group.MapInvalidateOccurrenceEndpoints();
        group.MapGetOccurrenceEndpoints();
        group.MapListOccurrencesEndpoints();
        group.MapListOccurrenceHistoryEndpoints();
        group.MapChangeOccurrenceStatusEndpoints();
        group.MapResolveOccurrenceEndpoints();
        return group;
    }
}
