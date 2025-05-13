using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogServiceOwnerLabels.Set;

public sealed class SetServiceOwnerLabelsEndpointSummary : Summary<SetServiceOwnerLabelsEndpoint>
{
    public SetServiceOwnerLabelsEndpointSummary()
    {
        Summary = "Replace the service owner labels for a dialog";
        Description = $"""
                       Replace the service owner labels for a dialog.

                       {Constants.SwaggerSummary.OptimisticConcurrencyNote}
                       """;
        ResponseHeaders = [HttpResponseHeaderExamples.NewServiceOwnerContextETagHeader(StatusCodes.Status204NoContent)];
        Responses[StatusCodes.Status204NoContent] = Constants.SwaggerSummary.Updated.FormatInvariant(nameof(ServiceOwnerLabel));
        Responses[StatusCodes.Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[StatusCodes.Status401Unauthorized] =
            Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure.FormatInvariant(AuthorizationScope
                .ServiceProvider);
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[StatusCodes.Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[StatusCodes.Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
