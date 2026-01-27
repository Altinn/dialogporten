using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;

internal static class AuthorizationPolicyBuilderExtensions
{
    public const string ScopeClaim = "scope";
    private const char ScopeClaimSeparator = ' ';

    extension(AuthorizationPolicyBuilder builder)
    {
        public AuthorizationPolicyBuilder RequireScope(string scope) =>
            builder.RequireAssertion(ctx => ctx.User.Claims
                .Where(x => x.Type == ScopeClaim)
                .Select(x => x.Value)
                .Any(scopeValue => scopeValue == scope || scopeValue
                    .Split(ScopeClaimSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Contains(scope)));

        public AuthorizationPolicyBuilder RequireValidConsumerClaim() =>
            builder.RequireAssertion(ctx => ctx.User.TryGetConsumerOrgNumber(out _));
    }
}
