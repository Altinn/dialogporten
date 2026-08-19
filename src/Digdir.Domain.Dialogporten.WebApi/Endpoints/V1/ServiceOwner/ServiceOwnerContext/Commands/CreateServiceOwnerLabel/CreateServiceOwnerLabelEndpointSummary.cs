using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.ServiceOwnerContext.Commands.CreateServiceOwnerLabel;

public sealed class CreateServiceOwnerLabelEndpointSummary : Summary<CreateServiceOwnerLabelEndpoint>
{
    public CreateServiceOwnerLabelEndpointSummary()
    {
        Summary = "Add a service owner label to a dialog";
        Description = $"""
                       Add a label to the service owner context.
                       {Constants.SwaggerSummary.OptimisticConcurrencyNote}
                       """;
        ResponseHeaders = [HttpResponseHeaderExamples.NewServiceOwnerContextETagHeader(Status204NoContent)];
        Responses[Status204NoContent] = Constants.SwaggerSummary.Updated.FormatInvariant(nameof(DialogServiceOwnerLabel));
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = OpenApiExtrasAttribute.Get401Error<CreateServiceOwnerLabelEndpoint>();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
