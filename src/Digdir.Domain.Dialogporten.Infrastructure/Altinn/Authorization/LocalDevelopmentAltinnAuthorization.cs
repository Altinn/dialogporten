using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class LocalDevelopmentAltinnAuthorization : IAltinnAuthorization
{
    private static readonly string LocalSubParty = NorwegianOrganizationIdentifier.PrefixWithSeparator + "123456789";

    private readonly IDialogDbContext _db;

    public LocalDevelopmentAltinnAuthorization(IDialogDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public Task<DialogDetailsAuthorizationResult> GetDialogDetailsAuthorization(
        DialogEntity dialogEntity,
        CancellationToken __) =>
        // Allow everything
        Task.FromResult(new DialogDetailsAuthorizationResult { AuthorizedAltinnActions = dialogEntity.GetAltinnActions() });

    public async Task<DialogSearchAuthorizationResult> GetAuthorizedResourcesForSearch(List<string> constraintParties, List<string> serviceResources,
        CancellationToken cancellationToken = default)
    {

        // constraintParties and serviceResources are passed from the client as query parameters
        // If one and/or the other is supplied this will limit the resources and parties to the ones supplied
        var dialogData = await _db.Dialogs
            .Select(dialog => new { dialog.Party, dialog.ServiceResource })
            .WhereIf(constraintParties.Count != 0, dialog => constraintParties.Contains(dialog.Party))
            .WhereIf(serviceResources.Count != 0, dialog => serviceResources.Contains(dialog.ServiceResource))
            .Distinct()
            .ToListAsync(cancellationToken);

        // Keep the number of parties and resources reasonable
        var allParties = dialogData.Select(x => x.Party).Distinct().Take(1000).ToList();
        var allResources = dialogData.Select(x => x.ServiceResource).Distinct().Take(1000).ToHashSet();

        var authorizedResources = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = allParties.ToDictionary(party => party, _ => allResources)
        };

        return authorizedResources;
    }

    public async Task<AuthorizedPartiesResult> GetAuthorizedParties(IPartyIdentifier authenticatedParty, bool _ = false, CancellationToken __ = default)
        => await Task.FromResult(new AuthorizedPartiesResult
        {
            AuthorizedParties = [new()
            {
                Name = "Local Party",
                Party = authenticatedParty.FullId,
                PartyUuid = Guid.NewGuid(),
                IsCurrentEndUser = true,
                SubParties = [
                    new()
                    {
                        Name = "Local Sub Party",
                        Party = LocalSubParty,
                        PartyUuid = Guid.NewGuid(),
                        IsCurrentEndUser = true
                    }
                ]
            }]
        });

    public async Task<AuthorizedPartiesResult> GetAuthorizedPartiesForLookup(
        IPartyIdentifier authenticatedParty,
        List<string> constraintParties,
        CancellationToken cancellationToken = default)
    {
        var authorizedResources = await _db.Dialogs
            .AsNoTracking()
            .Select(x => x.ServiceResource)
            .Distinct()
            .ToListAsync(cancellationToken);

        var parties = (constraintParties.Count > 0
            ? constraintParties.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : [authenticatedParty.FullId])
            .Concat([LocalSubParty])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthorizedPartiesResult
        {
            AuthorizedParties = parties
                .Select((party, index) => new AuthorizedParty
                {
                    Name = "Local Party",
                    Party = party,
                    PartyUuid = Guid.NewGuid(),
                    PartyId = index + 1,
                    IsCurrentEndUser = string.Equals(party, authenticatedParty.FullId, StringComparison.OrdinalIgnoreCase),
                    AuthorizedResources = [.. authorizedResources],
                    AuthorizedRolesAndAccessPackages = [],
                    AuthorizedInstances = []
                })
                .ToList()
        };
    }

    public Task<bool> HasListAuthorizationForDialog(DialogEntity _, CancellationToken __) => Task.FromResult(true);

    public bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel) => true;
    public Task<bool> UserHasRequiredAuthLevel(string serviceResource, CancellationToken _) => Task.FromResult(true);
}
