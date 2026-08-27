using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.NotificationCondition;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;


namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.NotificationCondition;

public sealed class NotificationConditionEndpointSummary : Summary<NotificationConditionEndpoint, NotificationConditionQuery>
{
    public NotificationConditionEndpointSummary()
    {
        Summary = "Returns a boolean value based on conditions used to determine if a notification is to be sent";
        Description = """
                      Used by Altinn Notification only. Takes a dialogId and returns a boolean value based on conditions used to determine if a notification is to be sent.
                      """;
        Responses[Status200OK] = "Successfully returned the notification determination.";
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<NotificationConditionEndpoint>();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
