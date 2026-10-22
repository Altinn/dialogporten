using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.SearchRelations;

public sealed class SearchDialogRelationsEndpointSummary : Summary<SearchDialogRelationsEndpoint>
{
    public SearchDialogRelationsEndpointSummary()
    {
        Summary = "Gets the relations of a dialog";
        Description = """
                      Gets the relations of a dialog the caller owns, as related dialog ids and relation types seen from the dialog. Ordered by the related dialog's id, which follows creation order.

                      The list is not filtered: a relation may point at a dialog owned by another service owner. Look up such a dialog separately if you own it.
                      """;
        Responses[Status200OK] = "The relations of the dialog.";
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<SearchDialogRelationsEndpoint>().Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
