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
    string GetDialogToken(DialogEntity dialog, DialogDetailsAuthorizationResult authorizationResult, string issuerVersion);

    /// <summary>
    /// Generates a token scoped to a single authorization-context-carrying entity, asserting the single
    /// PDP-verified grant (action, effective resource) along with the parties it was permitted for.
    /// </summary>
    string GetDialogContextToken(
        DialogEntity dialog,
        AuthorizedCheck authorizedCheck,
        Guid entityId,
        string entityType,
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

    public string GetDialogToken(DialogEntity dialog, DialogDetailsAuthorizationResult authorizationResult,
        string issuerVersion)
    {
        var claims = GetBaseClaims(dialog);
        claims[DialogTokenClaimTypes.Actions] = GetAuthorizedActions(authorizationResult);
        AddIssuerAndLifetimeClaims(claims, issuerVersion);

        return _compactJwsGenerator.GetCompactJws(claims, DialogTokenTypes.DialogToken);
    }

    public string GetDialogContextToken(
        DialogEntity dialog,
        AuthorizedCheck authorizedCheck,
        Guid entityId,
        string entityType,
        string issuerVersion)
    {
        var claims = GetBaseClaims(dialog);
        claims[DialogTokenClaimTypes.EntityId] = entityId;
        claims[DialogTokenClaimTypes.EntityType] = entityType;
        claims[DialogTokenClaimTypes.Actions] = authorizedCheck.Check.Action;

        // The effective resource for the grant; absent when the check applies to the dialog's own resource.
        var resource = authorizedCheck.Check.Resource.ServiceResource
                       ?? authorizedCheck.Check.Resource.AdditionalResourceAttribute;
        if (resource is not null)
        {
            claims[DialogTokenClaimTypes.EffectiveResource] = resource;
        }

        claims[DialogTokenClaimTypes.PermittedParties] = authorizedCheck.PermittedParties;
        AddIssuerAndLifetimeClaims(claims, issuerVersion);

        return _compactJwsGenerator.GetCompactJws(claims, DialogTokenTypes.DialogContextToken);
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
                    // The dialog token is frozen at legacy semantics: grants for authorization contexts
                    // are expressed exclusively through per-entity context tokens.
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
/// JOSE "typ" header values distinguishing the token types issued by Dialogporten (RFC 8725 explicit typing;
/// per RFC 7515 a value without '/' is shorthand for "application/&lt;value&gt;").
/// </summary>
public static class DialogTokenTypes
{
    /// <summary>
    /// The dialog token deliberately keeps the generic "JWT" type in v1. Explicit typing per RFC 8725 would
    /// suggest "dialogtoken+jwt", but changing an already-issued token's type is a silent breaking change for
    /// receivers that assert typ == "JWT" (a JWT library configured with an explicit valid-types list, or
    /// Nimbus' DefaultJWTProcessor, which permits only "JWT" or an absent type by default), and there is no
    /// transition window available: the type is issued, not negotiated. Since the value only has to
    /// <em>differ</em> between the token types signed by this key to keep them apart, retyping the dialog
    /// token would break such receivers without protecting anyone: those asserting "JWT" already reject the
    /// other types, and those ignoring "typ" cannot be protected retroactively either way.
    /// Switching to "dialogtoken+jwt" belongs to a future major version, where it can ride the issuer version
    /// already carried in the "iss" claim.
    /// </summary>
    public const string DialogToken = "JWT";

    public const string DialogContextToken = "dialogcontexttoken+jwt";
}

/// <summary>
/// Entity type discriminators for the <see cref="DialogTokenClaimTypes.EntityType"/> claim in context tokens.
/// </summary>
public static class DialogContextTokenEntityTypes
{
    public const string ApiAction = "apiaction";
    public const string GuiAction = "guiaction";
    public const string Attachment = "attachment";
    public const string Transmission = "transmission";
    public const string TransmissionAttachment = "transmissionattachment";
    public const string TransmissionNavigationalAction = "navigationalaction";
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

    // Context-token-only claims
    public const string EntityId = "e";
    public const string EntityType = "t";
    public const string EffectiveResource = "r";
    public const string PermittedParties = "pp";
}
