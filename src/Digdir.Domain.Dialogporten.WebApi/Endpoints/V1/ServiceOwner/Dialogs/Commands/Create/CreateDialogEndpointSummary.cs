using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Create;

public sealed class CreateDialogEndpointSummary : Summary<CreateDialogEndpoint>
{
    public CreateDialogEndpointSummary()
    {
        Summary = "Creates a new dialog";
        Description = """
                      The dialog is created with the given configuration.

                      For detailed information on validation rules, see [the source for create dialog validators](https://github.com/Altinn/dialogporten/tree/main/src/Digdir.Domain.Dialogporten.Application/Features/V1/ServiceOwner/Dialogs/Commands/Create/Validators)
                      """;

        ResponseExamples[Status201Created] = "018bb8e5-d9d0-7434-8ec5-569a6c8e01fc";

        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status201Created)];
        Responses[Status201Created] = Constants.SwaggerSummary.Created.FormatInvariant("aggregate");
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<CreateDialogEndpoint>()
            .Or(Constants.SwaggerSummary.DialogCreationNotAllowed)
            .Build();
        Responses[Status409Conflict] = Constants.SwaggerSummary.IdempotentKeyConflict.FormatInvariant("01941821-ffca-73a1-9335-435a882be014");
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
