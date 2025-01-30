using System.Collections.ObjectModel;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Authorization;

internal static class AuthorizationPolicy
{
    public const string EndUser = "enduser";
    public const string ServiceProvider = "serviceprovider";
    public const string NotificationConditionCheck = "notificationConditionCheck";
    public const string ServiceProviderSearch = "serviceproviderSearch";
    public const string Testing = "testing";
}

internal static class AuthorizationScope
{
    public const string EndUser = "digdir:dialogporten";
    public const string EndUserNoConsent = "digdir:dialogporten.noconsent";
    public const string ServiceProvider = "digdir:dialogporten.serviceprovider";
    public const string ServiceProviderSearch = "digdir:dialogporten.serviceprovider.search";
    public const string Testing = "digdir:dialogporten.developer.test";
    public const string NotificationConditionCheck = "altinn:system/notifications.condition.check";

    internal static readonly Lazy<IReadOnlyCollection<string>> AllScopes = new(GetAll);

    private static ReadOnlyCollection<string> GetAll() =>
        typeof(AuthorizationScope)
            .GetFields()
            .Where(x => x.IsLiteral && !x.IsInitOnly && x.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue()!)
            .ToList()
            .AsReadOnly();
}
