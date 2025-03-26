using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

public static class Constants
{
    public const string MainResource = "main";
    public const string ReadAction = "read";
    public const string TransmissionReadAction = "transmissionread";
    public static readonly Uri UnauthorizedUri = new("urn:dialogporten:unauthorized");

    public const string IdportenLoaSubstantial = "idporten-loa-substantial";
    public const string IdportenLoaHigh = "idporten-loa-high";
    public const string AltinnAuthLevelTooLow = "Altinn authentication level too low.";

    public const string SilentUpdateRequiresAdminScope = "IsSilentUpdate requires service owner admin scope.";

    public static readonly ImmutableArray<string> SupportedResourceTypes =
    [
        "GenericAccessResource",
        "AltinnApp",
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
