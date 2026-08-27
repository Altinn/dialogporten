using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;

public sealed class UpdateDialogTransmissionEndpointSummary : Summary<UpdateDialogTransmissionEndpoint>
{
    public UpdateDialogTransmissionEndpointSummary()
    {
        Summary = "Replaces a transmission";
        Description = $"""
                       Replaces a given transmission with the supplied model.

                       {Constants.SwaggerSummary.OptimisticConcurrencyNote}
                       """;

        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status204NoContent)];
        Responses[Status204NoContent] = Constants.SwaggerSummary.Updated.FormatInvariant("transmission");
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<UpdateDialogTransmissionEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialogForChildEntity
            .FormatInvariant("update");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogTransmissionNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
