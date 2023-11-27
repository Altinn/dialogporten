﻿using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Authorization;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class LocalDevelopmentAltinnAuthorization : IAltinnAuthorization
{
    public async Task<DialogSearchAuthorizationResponse> PerformDialogSearchAuthorization(DialogSearchAuthorizationRequest request, CancellationToken cancellationToken)
    {
        // TODO! Perhaps just get all parties and resources from the database and return those?

        var authorizedResources = new DialogSearchAuthorizationResponse
        {
            ResourcesForParties = new Dictionary<string, List<string>>
            {
                ["/org/991825827"] = new() { "urn:altinn:resource:super-simple-service" },
                ["/person/07874299582"] = new() { "urn:altinn:resource:super-simple-service2", "urn:altinn:resource:ttd-altinn-events-automated-tests" },
            },
            DialogIds = new List<Guid> { Guid.Parse("0ab48b01-74d4-3770-b91d-79f99fb16a5a"), Guid.Parse("0ab48b01-bbca-8873-a3fe-518ce47532ce") }

        };

        return await Task.FromResult(authorizedResources);
    }

    public Task<DialogDetailsAuthorizationResponse> PerformDialogDetailsAuthorization(DialogDetailsAuthorizationRequest request, CancellationToken cancellationToken)
    {
        // Just allow everything that was requested
        var response = new DialogDetailsAuthorizationResponse()
        {
            AuthorizedActions = request.Actions,
            AuthorizedAuthorizationAttributes = request.AuthorizationAttributes
        };

        return Task.FromResult(response);
    }
}
