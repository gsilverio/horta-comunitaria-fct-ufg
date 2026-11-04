using HortaComunitaria.Api.Features.Activities.CorrectActivity;
using HortaComunitaria.Api.Features.Activities.CreateParticipant;
using HortaComunitaria.Api.Features.Activities.UpdateParticipant;
using HortaComunitaria.Api.Features.Activities.ListActivities;
using HortaComunitaria.Api.Features.Activities.InvalidateActivity;
using HortaComunitaria.Api.Features.Activities.InactivateParticipant;
using HortaComunitaria.Api.Features.Activities.GetActivity;
using HortaComunitaria.Api.Features.Activities.GetParticipant;
using HortaComunitaria.Api.Features.Activities.RegisterActivity;
using HortaComunitaria.Api.Features.Activities.ListActivityHistory;
using HortaComunitaria.Api.Features.Activities.ListParticipants;
using HortaComunitaria.Api.Features.Activities.ListParticipantHistory;

using HortaComunitaria.Api.Features.Activities.ListParticipantCatalog;

namespace HortaComunitaria.Api.Features.Activities;

internal static class ActivitiesEndpointRegistration
{
    internal static RouteGroupBuilder MapActivitiesEndpoints(this RouteGroupBuilder group) => group.MapListParticipantCatalogEndpoints().MapListParticipantsEndpoints().MapCreateParticipantEndpoints().MapUpdateParticipantEndpoints().MapGetParticipantEndpoints().MapInactivateParticipantEndpoints().MapListParticipantHistoryEndpoints().MapRegisterActivityEndpoints().MapCorrectActivityEndpoints().MapInvalidateActivityEndpoints().MapGetActivityEndpoints().MapListActivitiesEndpoints().MapListActivityHistoryEndpoints();
}

