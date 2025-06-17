using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.DialogActivities.Queries.NotificationCondition;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;


namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogActivities.NotificationCondition;

public sealed class SearchDialogActivityEndpointSummary : Summary<NotificationConditionEndpoint, NotificationConditionQuery>
{
    public SearchDialogActivityEndpointSummary()
    {
        Summary = "Returns a boolean value based on conditions used to determine if a notification is to be sent";
        Description = """
                      Used by Altinn Notification only. Takes a dialogId and returns a boolean value based on conditions used to determine if a notification is to be sent.
                      """;
        Responses[StatusCodes.Status200OK] = "Successfully returned the notification determination.";
        Responses[StatusCodes.Status401Unauthorized] = Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure.FormatInvariant(AuthorizationScope.NotificationConditionCheck);
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
