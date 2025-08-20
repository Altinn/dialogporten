using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabel;

public sealed class GetServiceOwnerLabelEndpointSummary : Summary<GetServiceOwnerLabelEndpoint>
{
    public GetServiceOwnerLabelEndpointSummary()
    {
        Summary = "Retrieve service owner labels for a dialog.";
        Description = "Fetches all labels associated with the service owner context of a specific dialog.";
        ResponseHeaders = [HttpResponseHeaderExamples.NewServiceOwnerContextETagHeader(StatusCodes.Status204NoContent)];
        Responses[StatusCodes.Status200OK] = "Successfully retrieved the service owner labels.";
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[StatusCodes.Status401Unauthorized] =
            Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure.FormatInvariant(AuthorizationScope.ServiceProvider);
    }
}
