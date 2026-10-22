using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.SearchRelations;

public sealed class SearchDialogRelationsEndpointSummary : Summary<SearchDialogRelationsEndpoint>
{
    public SearchDialogRelationsEndpointSummary()
    {
        Summary = "Gets the related dialogs of a dialog";
        Description = """
                      Gets the dialogs related to a dialog (its parent, children and related dialogs), with the relation type seen from the dialog, as a subset of the dialog search list items. Ordered by the related dialog's id, which follows creation order.

                      Authorization follows the dialog search: a related dialog is included exactly when it would appear in the user's own search. Related dialogs the user cannot access are left out silently, so a dialog with hasRelations set may return an empty list. Opening a related dialog goes through the normal dialog details endpoint.
                      """;
        Responses[Status200OK] = "The related dialogs the user may see.";
        Responses[Status400BadRequest] = Constants.SwaggerSummary.ValidationError;
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<SearchDialogRelationsEndpoint>().Build();
        Responses[Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
