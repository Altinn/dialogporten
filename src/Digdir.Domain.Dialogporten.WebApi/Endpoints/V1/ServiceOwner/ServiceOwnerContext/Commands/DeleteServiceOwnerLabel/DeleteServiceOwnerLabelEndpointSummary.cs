using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.ServiceOwnerContext.Commands.DeleteServiceOwnerLabel;

public sealed class DeleteServiceOwnerLabelEndpointSummary : Summary<DeleteServiceOwnerLabelEndpoint>
{
    public DeleteServiceOwnerLabelEndpointSummary()
    {
        Summary = "Delete a service owner label for a dialog";
        Description = "Removes a specific label from the service owner context of a dialog. If the label does not exist, a NotFound response is returned.";
        ResponseHeaders = [HttpResponseHeaderExamples.NewServiceOwnerContextETagHeader(StatusCodes.Status204NoContent)];
        Responses[StatusCodes.Status204NoContent] = "Successfully deleted the service owner label.";
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.ServiceOwnerLabelNotFound;
    }
}
