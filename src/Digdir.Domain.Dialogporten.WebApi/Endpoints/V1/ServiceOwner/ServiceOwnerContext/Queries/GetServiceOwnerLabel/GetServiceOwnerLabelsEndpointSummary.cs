using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabel;

public sealed class GetServiceOwnerLabelsEndpointSummary : Summary<GetServiceOwnerLabelEndpoint>
{
    public GetServiceOwnerLabelsEndpointSummary()
    {
        Summary = "Retrieve service owner labels for a dialog.";
        Description = "Fetches all labels associated with the service owner context of a specific dialog.";
        ResponseHeaders = [HttpResponseHeaderExamples.NewServiceOwnerContextETagHeader(Status200OK)];
        Responses[Status200OK] = "Successfully retrieved the service owner labels.";
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<GetServiceOwnerLabelEndpoint>().Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
