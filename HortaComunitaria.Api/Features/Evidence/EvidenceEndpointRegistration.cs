using HortaComunitaria.Api.Features.Evidence.CorrectEvidence;
using HortaComunitaria.Api.Features.Evidence.GetEvidenceContent;
using HortaComunitaria.Api.Features.Evidence.InvalidateEvidence;
using HortaComunitaria.Api.Features.Evidence.ListEvidences;
using HortaComunitaria.Api.Features.Evidence.UploadEvidence;

namespace HortaComunitaria.Api.Features.Evidence;

internal static class EvidenceEndpointRegistration
{
    internal static RouteGroupBuilder MapEvidenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapUploadEvidenceEndpoints();
        group.MapListEvidencesEndpoints();
        group.MapGetEvidenceContentEndpoints();
        group.MapCorrectEvidenceEndpoints();
        group.MapInvalidateEvidenceEndpoints();
        return group;
    }
}
