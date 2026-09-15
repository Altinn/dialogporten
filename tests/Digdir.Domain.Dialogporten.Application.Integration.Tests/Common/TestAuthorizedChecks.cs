using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal static class TestAuthorizedChecks
{
    /// <summary>
    /// Builds an authorized legacy-style check, mirroring the semantics of the old
    /// AltinnAction(name, authorizationAttribute) test fixtures: a null (or "main")
    /// authorization attribute targets the main resource.
    /// </summary>
    internal static AuthorizedCheck Authorized(string name, string? authorizationAttribute = null) =>
        AuthorizedCheck.FullyPermitted(new AuthorizationCheck(
            name,
            AuthorizationResourceSpec.FromLegacyAuthorizationAttribute(
                authorizationAttribute == Constants.MainResource ? null : authorizationAttribute),
            []));

    /// <summary>
    /// Builds an authorized context check for the given carrier entity, using the same normalization
    /// as the production evaluation (so decoration-time lookups match).
    /// </summary>
    internal static AuthorizedCheck Authorized(AuthorizationCheck check) => AuthorizedCheck.FullyPermitted(check);
}
