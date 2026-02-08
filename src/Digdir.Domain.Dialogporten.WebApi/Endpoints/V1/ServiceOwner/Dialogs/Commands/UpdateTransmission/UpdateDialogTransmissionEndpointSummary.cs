using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
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

        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(StatusCodes.Status204NoContent)];
        Responses[StatusCodes.Status204NoContent] = Constants.SwaggerSummary.Updated.FormatInvariant("transmission");
        Responses[StatusCodes.Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[StatusCodes.Status401Unauthorized] =
            Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure.FormatInvariant(AuthorizationScope.ServiceProvider);
        Responses[StatusCodes.Status403Forbidden] =
            Constants.SwaggerSummary.AccessDeniedToDialogForChildEntity.FormatInvariant("update");
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[StatusCodes.Status409Conflict] = Constants.SwaggerSummary.Conflict;
        Responses[StatusCodes.Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[StatusCodes.Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[StatusCodes.Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
