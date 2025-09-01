using System.Diagnostics;
using System.Globalization;
using System.Text;
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
        _applicationSettings = applicationSettings.Value ?? throw new ArgumentNullException(nameof(applicationSettings));
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _compactJwsGenerator = compactJwsGenerator ?? throw new ArgumentNullException(nameof(compactJwsGenerator));
    }

    public string GetDialogToken(DialogEntity dialog, DialogDetailsAuthorizationResult authorizationResult,
        string issuerVersion)
    {
        var claimsPrincipal = _user.GetPrincipal();
        var now = _clock.UtcNowOffset.ToUnixTimeSeconds();
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
        claims[DialogTokenClaimTypes.Actions] = GetAuthorizedActions(authorizationResult);
        claims[DialogTokenClaimTypes.Issuer] = _applicationSettings.Dialogporten.BaseUri.AbsoluteUri.TrimEnd('/') + issuerVersion;
        claims[DialogTokenClaimTypes.IssuedAt] = now;
        claims[DialogTokenClaimTypes.NotBefore] = now;
        claims[DialogTokenClaimTypes.Expires] = now + (long)_tokenLifetime.TotalSeconds;

        return _compactJwsGenerator.GetCompactJws(claims);
    }

    private static string GetAuthorizedActions(DialogDetailsAuthorizationResult authorizationResult)
    {
        if (authorizationResult.AuthorizedAltinnActions.Count == 0)
        {
            return string.Empty;
        }

        var actions = new StringBuilder();
        foreach (var (action, resource) in authorizationResult.AuthorizedAltinnActions)
        {
            actions.Append(action);
            if (resource != Authorization.Constants.MainResource)
            {
                actions.Append(CultureInfo.InvariantCulture, $",{resource}");
            }

            actions.Append(';');
        }

        // Remove trailing semicolon
        actions.Remove(actions.Length - 1, 1);

        return actions.ToString();
    }
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
}
