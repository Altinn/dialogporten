using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Freeze;

public sealed class FreezeDialogEndpointSummary : Summary<FreezeDialogEndpoint>
{
    public FreezeDialogEndpointSummary()
    {
        Summary = "Freezes a dialog";
        Description = """
                      Freezes a given dialog.
                      The dialog cannot be updated/deleted by the service owner (but can still be altered via admin-scope) when frozen
                      """;
        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status204NoContent)];
        Responses[Status204NoContent] = Constants.SwaggerSummary.Frozen.FormatInvariant("aggregate");
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<FreezeDialogEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialog.FormatInvariant("freeze");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
    }

}
