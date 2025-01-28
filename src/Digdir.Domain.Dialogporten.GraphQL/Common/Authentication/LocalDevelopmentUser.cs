using System.Collections.ObjectModel;
using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;

internal sealed class LocalDevelopmentUser : IUser
{
    private readonly ClaimsPrincipal _principal = new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "Local Development User"),
        new Claim(ClaimTypes.NameIdentifier, "local-development-user"),
        new Claim("pid", "03886595947"),
        new Claim("scope", string.Join(" ", AuthorizationScope.AllScopes.Value)),
        new Claim("consumer",
            """
            {
                "authority": "iso6523-actorid-upis",
                "ID": "0192:991825827"
            }
            """)
    ]));

    public ClaimsPrincipal GetPrincipal() => _principal;
}


internal static class AuthorizationScope
{
    public const string EndUser = "digdir:dialogporten";
    public const string ServiceProvider = "digdir:dialogporten.serviceprovider";
    public const string ServiceProviderSearch = "digdir:dialogporten.serviceprovider.search";
    public const string Testing = "digdir:dialogporten.developer.test";
    public const string Correspondence = "digdir:dialogporten.correspondence";

    internal static readonly Lazy<IReadOnlyCollection<string>> AllScopes = new(GetAll);

    private static ReadOnlyCollection<string> GetAll() =>
        typeof(AuthorizationScope)
            .GetFields()
            .Where(x => x.IsLiteral && !x.IsInitOnly && x.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue()!)
            .ToList()
            .AsReadOnly();
}
