using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

public sealed class CreateDialogActivityEndpointSummary : Summary<CreateDialogActivityEndpoint>
{
    public CreateDialogActivityEndpointSummary()
    {
        Summary = "Adds an activity to a dialog's activity history";
        Description = $"""
                       The activity is created with the given configuration.

                       Creating an activity does not count as a content change, 
                       so it leaves the end user's system labels untouched; a dialog in 'Bin' or 'Archive' stays there.

                       A transmission can only be opened once. If the activity is of type 'TransmissionOpened' and the
                       referenced transmission already has a 'TransmissionOpened' activity (regardless of who performed it),
                       the request is still validated, but no new activity is stored. The response is still a success,
                       containing the id of the existing 'TransmissionOpened' activity and the dialog's current, unchanged revision.
                       No new revision or event is produced. A self-supplied activity id is not stored in this case.

                       {Constants.SwaggerSummary.OptimisticConcurrencyNote}
                       """;

        ResponseExamples[Status201Created] = "018bb8e5-d9d0-7434-8ec5-569a6c8e01fc";

        ResponseHeaders = [HttpResponseHeaderExamples.NewDialogETagHeader(Status201Created)];
        Responses[Status201Created] = Constants.SwaggerSummary.Created.FormatInvariant("activity");
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<CreateDialogActivityEndpoint>()
            .Or(Constants.SwaggerSummary.AccessDeniedToDialogForChildEntity.FormatInvariant("create"))
            .Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
        Responses[Status409Conflict] = new ConflictMessageBuilder()
            .ConcurrentOperationRejected()
            .Build();
        Responses[Status410Gone] = Constants.SwaggerSummary.DialogDeleted;
        Responses[Status412PreconditionFailed] = Constants.SwaggerSummary.RevisionMismatch;
        Responses[Status422UnprocessableEntity] = Constants.SwaggerSummary.DomainError;
    }
}
