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

    public string GetDialogToken(DialogEntity dialog, DialogDetailsAuthorizationResult authorizationResult, string issuerVersion)
    {
        var claimsPrincipal = _user.GetPrincipal();
        var now = _clock.UtcNowOffset.ToUnixTimeSeconds();

        var claims = new Dictionary<string, object?>
        {
            { DialogTokenClaimTypes.JwtId, Guid.NewGuid() },
            { DialogTokenClaimTypes.AuthenticatedParty, GetAuthenticatedParty() },
            { DialogTokenClaimTypes.AuthenticationLevel, claimsPrincipal.GetAuthenticationLevel() },
            { DialogTokenClaimTypes.DialogParty, dialog.Party },
            { DialogTokenClaimTypes.ServiceResource, dialog.ServiceResource },
            { DialogTokenClaimTypes.DialogId, dialog.Id },
            { DialogTokenClaimTypes.Actions, GetAuthorizedActions(authorizationResult) },
            { DialogTokenClaimTypes.Issuer, _applicationSettings.Dialogporten.BaseUri.AbsoluteUri.TrimEnd('/') + issuerVersion },
            { DialogTokenClaimTypes.IssuedAt, now },
            { DialogTokenClaimTypes.NotBefore, now },
            { DialogTokenClaimTypes.Expires, now + (long)_tokenLifetime.TotalSeconds }
        };

        if (claimsPrincipal.TryGetSupplierOrgNumber(out var supplierOrgNumber))
        {
            claims.Add(DialogTokenClaimTypes.SupplierParty, NorwegianOrganizationIdentifier.PrefixWithSeparator + supplierOrgNumber);
        }

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

    private string GetAuthenticatedParty()
    {
        if (_user.TryGetPid(out var pid))
        {
            return NorwegianPersonIdentifier.PrefixWithSeparator + pid;
        }

        if (_user.TryGetOrganizationNumber(out var orgNumber))
        {
            return NorwegianOrganizationIdentifier.PrefixWithSeparator + orgNumber;
        }

        throw new InvalidOperationException("User must have either pid or org number");
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
    public const string ServiceResource = "s";
    public const string DialogId = "i";
    public const string Actions = "a";
}
