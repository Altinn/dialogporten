using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;

public sealed class CreateDialogTransmissionEndpointSummary : Summary<CreateDialogTransmissionEndpoint>
{
    public CreateDialogTransmissionEndpointSummary()
    {
        Summary = "Adds a transmission to a dialog";
        Description = $"""
                       The transmission is created with the given configuration.

                       {Constants.SwaggerSummary.OptimisticConcurrencyNote}
                       """;

        ResponseExamples[Status201Created] = "018bb8e5-d9d0-7434-8ec5-569a6c8e01fc";

        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status201Created)];
        Responses[Status201Created] = Constants.SwaggerSummary.Created.FormatInvariant("transmission");
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<CreateDialogTransmissionEndpoint>();
        Responses[Status403Forbidden] = Constants.SwaggerSummary.AccessDeniedToDialogForChildEntity.FormatInvariant("create");
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
