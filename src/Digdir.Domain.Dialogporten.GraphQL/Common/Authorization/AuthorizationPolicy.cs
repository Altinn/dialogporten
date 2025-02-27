namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;

internal static class AuthorizationPolicy
{
    public const string EndUser = "enduser";
    public const string EndUserSubscription = "enduserSubscription";
    public const string ServiceProvider = "serviceprovider";
    public const string ServiceProviderSearch = "serviceproviderSearch";
    public const string Testing = "testing";
}
