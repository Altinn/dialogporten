using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Create;

public sealed class CreateDialogEndpointSummary : Summary<CreateDialogEndpoint>
{
    public CreateDialogEndpointSummary()
    {
        Summary = "Creates a new dialog";
        Description = """
                      The dialog is created with the given configuration.

                      For detailed information on validation rules, see [the source for CreateDialogCommandValidator](https://github.com/altinn/dialogporten/blob/main/src/Digdir.Domain.Dialogporten.Application/Features/V1/ServiceOwner/Dialogs/Commands/Create/CreateDialogCommandValidator.cs)
                      """;

        ResponseExamples[StatusCodes.Status201Created] = "018bb8e5-d9d0-7434-8ec5-569a6c8e01fc";

        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(StatusCodes.Status201Created)];
        Responses[StatusCodes.Status201Created] = Constants.SwaggerSummary.Created.FormatInvariant("aggregate");
        Responses[StatusCodes.Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[StatusCodes.Status401Unauthorized] = Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure.FormatInvariant(AuthorizationScope.ServiceProvider);
        Responses[StatusCodes.Status403Forbidden] = Constants.SwaggerSummary.DialogCreationNotAllowed;
        Responses[StatusCodes.Status409Conflict] = Constants.SwaggerSummary.IdempotentKeyConflict.FormatInvariant("01941821-ffca-73a1-9335-435a882be014");
        Responses[StatusCodes.Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
