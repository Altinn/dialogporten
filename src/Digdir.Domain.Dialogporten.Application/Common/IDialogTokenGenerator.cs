using System.Diagnostics;
using System.Globalization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Common;

public interface IDialogTokenGenerator
{
    /// <summary>
    /// Generates the dialog token: the legacy action grants in "a", and the references of every
    /// authorization-context-carrying entity the user is authorized for in "e" (omitted when empty).
    /// </summary>
    /// <param name="dialog">The dialog the token is issued for.</param>
    /// <param name="authorizationResult">The PDP result the action grants are derived from.</param>
    /// <param name="authorizedContextReferences">
    /// For each authorization context the user is authorized for, the carrying entity's id or the service
    /// owner supplied token reference. See <see cref="Authorization.AuthorizedContextReferences"/>.
    /// </param>
    /// <param name="issuerVersion">The API version suffix appended to the issuer.</param>
    string GetDialogToken(
        DialogEntity dialog,
        DialogDetailsAuthorizationResult authorizationResult,
        IReadOnlyCollection<string> authorizedContextReferences,
        string issuerVersion);
}

internal sealed class DialogTokenGenerator : IDialogTokenGenerator
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly IUser _user;
    private readonly IClock _clock;
    private readonly ICompactJwsGenerator _compactJwsGenerator;

    // Keep the lifetime semi-short to reduce the risk of token misuse
    // after rights revocation, whilst still making it possible for the
    // user to idle a reasonable amount of time before committing to an action.
    //
    // End user systems should make sure to re-request the dialog, upon
    // which a new token will be issued based on current authorization data.
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromMinutes(10);

    public DialogTokenGenerator(
        IOptions<ApplicationSettings> applicationSettings,
        IUser user,
        IClock clock,
        ICompactJwsGenerator compactJwsGenerator)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(compactJwsGenerator);

        var settings = applicationSettings.Value;
        ArgumentNullException.ThrowIfNull(settings, nameof(applicationSettings));

        _applicationSettings = settings;
        _user = user;
        _clock = clock;
        _compactJwsGenerator = compactJwsGenerator;
    }

    public string GetDialogToken(
        DialogEntity dialog,
        DialogDetailsAuthorizationResult authorizationResult,
        IReadOnlyCollection<string> authorizedContextReferences,
        string issuerVersion)
    {
        ArgumentNullException.ThrowIfNull(authorizedContextReferences);

        var claims = GetBaseClaims(dialog);
        claims[DialogTokenClaimTypes.Actions] = GetAuthorizedActions(authorizationResult);

        // Omitted rather than emitted empty, so dialogs that do not use authorization contexts issue a
        // token of exactly the pre-existing shape.
        if (authorizedContextReferences.Count > 0)
        {
            claims[DialogTokenClaimTypes.AuthorizedEntities] = authorizedContextReferences
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        AddIssuerAndLifetimeClaims(claims, issuerVersion);

        return _compactJwsGenerator.GetCompactJws(claims, DialogTokenTypes.DialogToken);
    }

    private Dictionary<string, object?> GetBaseClaims(DialogEntity dialog)
    {
        var claimsPrincipal = _user.GetPrincipal();
        var endUserPartyIdentifier = claimsPrincipal.GetEndUserPartyIdentifier();

        var claims = new Dictionary<string, object?>(15)
        {
            [DialogTokenClaimTypes.JwtId] = Guid.NewGuid()
        };

        // If we have authenticated a system user, we want the consumer organization number as the authenticated party
        // and adding the system user identifier as a separate claim along with the system user's organization.
        if (endUserPartyIdentifier is SystemUserIdentifier
            && claimsPrincipal.TryGetConsumerOrgNumber(out var consumerOrgNumber)
            && claimsPrincipal.TryGetSystemUserOrgNumber(out var systemUserOrgNumber))
        {
            claims[DialogTokenClaimTypes.AuthenticatedParty] = NorwegianOrganizationIdentifier.PrefixWithSeparator + consumerOrgNumber;
            claims[DialogTokenClaimTypes.SystemUserId] = endUserPartyIdentifier.FullId;
            claims[DialogTokenClaimTypes.SystemUserOrg] = NorwegianOrganizationIdentifier.PrefixWithSeparator + systemUserOrgNumber;
        }
        else
        {
            claims[DialogTokenClaimTypes.AuthenticatedParty] = endUserPartyIdentifier is not null
                ? endUserPartyIdentifier.FullId
                : throw new UnreachableException("Cannot create dialog token - missing end user claims.");
        }

        // If we have a supplier organization number from Maskinporten delegation ("supplier"), add it as a separate claim.
        if (claimsPrincipal.TryGetSupplierOrgNumber(out var supplierOrgNumber))
        {
            claims[DialogTokenClaimTypes.SupplierParty] = NorwegianOrganizationIdentifier.PrefixWithSeparator + supplierOrgNumber;
        }

        claims[DialogTokenClaimTypes.AuthenticationLevel] = claimsPrincipal.GetAuthenticationLevel();
        claims[DialogTokenClaimTypes.DialogParty] = dialog.Party;
        claims[DialogTokenClaimTypes.ServiceResource] = dialog.ServiceResource;
        claims[DialogTokenClaimTypes.DialogId] = dialog.Id;

        return claims;
    }

    private void AddIssuerAndLifetimeClaims(Dictionary<string, object?> claims, string issuerVersion)
    {
        var now = _clock.UtcNowOffset.ToUnixTimeSeconds();
        claims[DialogTokenClaimTypes.Issuer] = _applicationSettings.Dialogporten.BaseUri.AbsoluteUri.TrimEnd('/') + issuerVersion;
        claims[DialogTokenClaimTypes.IssuedAt] = now;
        claims[DialogTokenClaimTypes.NotBefore] = now;
        claims[DialogTokenClaimTypes.Expires] = now + (long)_tokenLifetime.TotalSeconds;
    }

    private static string GetAuthorizedActions(DialogDetailsAuthorizationResult authorizationResult)
    {
        var entries = new List<string>();
        foreach (var authorizedCheck in authorizationResult.AuthorizedChecks)
        {
            var check = authorizedCheck.Check;
            string entry;
            switch (check.Resource.Kind)
            {
                case AuthorizationResourceSpecKind.Main:
                    entry = check.Action;
                    break;

                case AuthorizationResourceSpecKind.Legacy:
                    // Preserve the legacy wire format exactly: a literal "main" attribute is
                    // indistinguishable from the main resource and serializes without a resource part.
                    entry = check.Resource.LegacyAuthorizationAttribute == Authorization.Constants.MainResource
                        ? check.Action
                        : string.Create(CultureInfo.InvariantCulture, $"{check.Action},{check.Resource.LegacyAuthorizationAttribute}");
                    break;

                case AuthorizationResourceSpecKind.Context:
                default:
                    // "a" is frozen at legacy semantics. A context may grant via another party or resource than
                    // the dialog's own, which an action name alone cannot express safely; context grants are
                    // instead listed per entity in "e".
                    continue;
            }

            if (!entries.Contains(entry, StringComparer.Ordinal))
            {
                entries.Add(entry);
            }
        }

        return string.Join(';', entries);
    }
}

/// <summary>
/// JOSE "typ" header value of the token issued by Dialogporten.
/// </summary>
public static class DialogTokenTypes
{
    /// <summary>
    /// The dialog token deliberately keeps the generic "JWT" type in v1. Explicit typing per RFC 8725 would
    /// suggest "dialogtoken+jwt", but changing an already-issued token's type is a silent breaking change for
    /// receivers that assert typ == "JWT" (a JWT library configured with an explicit valid-types list, or
    /// Nimbus' DefaultJWTProcessor, which permits only "JWT" or an absent type by default), and there is no
    /// transition window available: the type is issued, not negotiated. Switching to "dialogtoken+jwt" belongs
    /// to a future major version, where it can ride the issuer version already carried in the "iss" claim.
    /// </summary>
    public const string DialogToken = "JWT";
}

public static class DialogTokenClaimTypes
{
    public const string JwtId = "jti";
    public const string Issuer = "iss";
    public const string IssuedAt = "iat";
    public const string NotBefore = "nbf";
    public const string Expires = "exp";
    public const string AuthenticationLevel = "l";
    public const string AuthenticatedParty = "c";
    public const string DialogParty = "p";
    public const string SupplierParty = "u";
    public const string SystemUserId = "y";
    public const string SystemUserOrg = "o";
    public const string ServiceResource = "s";
    public const string DialogId = "i";
    public const string Actions = "a";

    /// <summary>
    /// Flat array of entity references, one per authorization context the user is authorized for: the id of the
    /// entity carrying the context (api action, gui action, attachment, transmission, transmission attachment or
    /// navigational action), or the service owner supplied token reference when the context has one. Omitted
    /// when there are none. A receiver checks that the entity a request targets is listed here.
    /// </summary>
    public const string AuthorizedEntities = "e";
}
