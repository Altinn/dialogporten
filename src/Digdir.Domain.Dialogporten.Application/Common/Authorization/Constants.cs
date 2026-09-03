using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

public static class Constants
{
    public const string MainResource = "main";
    public const string ReadAction = "read";

    /// <summary>
    /// Persisted in <c>DialogTransmission.AuthorizationAttribute</c> for transmissions that carry an
    /// authorization context, so that code predating authorization contexts keeps such transmissions
    /// hidden if this feature is rolled back: that code routes a bare, colon-free attribute to the
    /// "transmissionread" action, which no XACML policy defines anywhere, so the transmission is denied
    /// rather than falling through to the dialog's main resource. (Current code derives "read" for every
    /// legacy attribute, so the sentinel only protects a rollback that also restores that behaviour.)
    ///
    /// Never consulted when deciding access — <see cref="AuthorizationCheckBuilder"/> reads the context —
    /// and suppressed to null on every read surface, so it neither reaches a client nor breaks a
    /// GET → PUT round trip against the write model's context/attribute exclusivity rule. That suppression
    /// keys on the presence of a context, not on this value: it is also a well-formed attribute a service
    /// owner may supply on its own, and such an attribute must still round-trip. See
    /// <c>DialogTransmission.EffectiveLegacyAuthorizationAttribute</c>.
    /// </summary>
    public const string ExcludedTransmissionAttribute = "dp-excluded";
    public static readonly Uri UnauthorizedUri = new("urn:dialogporten:unauthorized");
    public static readonly Uri ExpiredUri = new("urn:dialogporten:expired");

    public const string IdportenLoaLow = "idporten-loa-low";
    public const string IdportenLoaSubstantial = "idporten-loa-substantial";
    public const string IdportenLoaHigh = "idporten-loa-high";
    public const string IdportenLoaEmail = "selfregistered-email";
    public const string AltinnAuthLevelTooLow = "Altinn authentication level too low.";

    public static readonly ImmutableArray<string> SupportedResourceTypes =
    [
        "GenericAccessResource",
        "AltinnApp",
        "MigratedApp",
        "CorrespondenceService"
    ];
}

public static class AuthorizationScope
{
    /// <summary>
    /// Needed to be able to modify (create/update/delete) correspondence service resources. Primarily used by the correspondence service.
    /// </summary>
    public const string CorrespondenceScope = "digdir:dialogporten.correspondence";

    /// <summary>
    /// Basic service owner scope. Needed to be able to modify (create/update/delete) dialogs owned by the authenticated service owner.
    /// </summary>
    public const string ServiceProvider = "digdir:dialogporten.serviceprovider";

    /// <summary>
    /// An extension to the service owner scope allowing access to the search endpoint.
    /// </summary>
    public const string ServiceProviderSearch = "digdir:dialogporten.serviceprovider.search";

    /// <summary>
    /// Allows technical corrections to existing transmissions using silent updates.
    /// </summary>
    public const string ServiceProviderChangeTransmissions = "digdir:dialogporten.serviceprovider.changetransmissions";

    /// <summary>
    /// Allows the modification (create/update/delete) of dialogs on behalf of all service owners regardless of the authenticated user.
    /// </summary>
    public const string ServiceOwnerAdminScope = "digdir:dialogporten.serviceprovider.admin";

    /// <summary>
    /// Allows the user to be able to provide HTML content as part of the dialog. This is used to migrate old correspondence messages to dialogs.
    /// </summary>
    public const string LegacyHtmlScope = "digdir:dialogporten.serviceprovider.legacyhtml";

    /// <summary>
    /// Basic end user scope. Needed to be able to access the end-user apis and read dialogs the end user is authorized to see.
    /// </summary>
    public const string EndUser = "digdir:dialogporten";

    /// <summary>
    /// Same as EndUser, but does not prompt the user with a consent dialog when logging in with IdPorten.
    /// </summary>
    public const string EndUserNoConsent = "digdir:dialogporten.noconsent";

    /// <summary>
    /// Gives access to the dialogs/{dialogId}/actions/should-send-notification endpoint.
    /// </summary>
    public const string NotificationConditionCheck = "altinn:system/notifications.condition.check";

    /// <summary>
    /// Gives access to hidden development endpoints. This scope is not available in production.
    /// </summary>
    public const string Testing = "digdir:dialogporten.developer.test";

    public static readonly Lazy<IReadOnlyCollection<string>> AllScopes = new(GetAll);
    private static ReadOnlyCollection<string> GetAll() =>
        typeof(AuthorizationScope)
            .GetFields()
            .Where(x => x.IsLiteral && !x.IsInitOnly && x.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue()!)
            .ToList()
            .AsReadOnly();
}
