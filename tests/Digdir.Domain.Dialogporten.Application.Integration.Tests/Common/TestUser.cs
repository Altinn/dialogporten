using System.Security.Claims;
using System.Security.Principal;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed class TestUser : IUser
{
    private static string DefaultPid => "22834498646";
    public static string DefaultParty => NorwegianPersonIdentifier.PrefixWithSeparator + DefaultPid;

    private static readonly ClaimsPrincipal DefaultPrincipal = new(TestUsers.Default);

    private ClaimsPrincipal? _override;

    public ClaimsPrincipal GetPrincipal() => _override ?? DefaultPrincipal;

    public void OverrideUser(ClaimsPrincipal principal) => _override = principal;

    public void Reset() => _override = null;
}

internal static class TestUsers
{
    public const string DefaultPid ="22834498646";
    // public static string DefaultParty => NorwegianPersonIdentifier.PrefixWithSeparator + DefaultPid;
    public static readonly ClaimsPrincipal Default = new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "Integration Test User"),
        new Claim("acr", Constants.IdportenLoaHigh),
        new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
        new Claim("pid", DefaultPid),
        new Claim("consumer",
            """
            {
                "authority": "iso6523-actorid-upis",
                "ID": "0192:991825827"
            }
            """)
    ]));
    
    public static readonly 

    public static ClaimsPrincipalBuilder FromDefault() => ClaimsPrincipalBuilder.From(Default);

    extension<TFlowStep>(TFlowStep flowStep) where TFlowStep : IFlowStep
    {
        public TFlowStep AsIntegrationTestUser(Action<ClaimsPrincipalBuilder>? configure = null) =>
            flowStep.AsUser(() => FromDefault()
                .Configure(configure)
                .Build());

        public TFlowStep AsCorrespondenceUser() => flowStep.AsUser(ClaimsPrincipalBuilder
            .Create(UserStore.IntegrationTestUser)
            .WithScope(AuthorizationScope.CorrespondenceScope)
            .Build());

        public TFlowStep AsAdminUser() => flowStep.AsUser(ClaimsPrincipalBuilder
            .Create(UserStore.IntegrationTestUser)
            .WithScope(AuthorizationScope.ServiceOwnerAdminScope)
            .Build());

        public TFlowStep AsCorrespondenceUser(Action<ClaimsPrincipalBuilder>? configure = null) =>
            flowStep.AsUser(() => FromDefault()
                .WithScope(AuthorizationScope.CorrespondenceScope)
                .Configure(configure)
                .Build());
        public TFlowStep AsAdminUser() => throw new NotImplementedException();
        public TFlowStep AsEndUser() => throw new NotImplementedException();
        public TFlowStep AsServiceOwnerUser() => throw new NotImplementedException();

        public TFlowStep AsUser(ClaimsPrincipal claimsPrincipal) =>
            flowStep.Do(_ => DialogApplication.User.OverrideUser(claimsPrincipal));

        public TFlowStep AsUser(Func<ClaimsPrincipal> claimsPrincipalBuilder) =>
            flowStep.Do(_ => DialogApplication.User.OverrideUser(claimsPrincipalBuilder()));
    }
}

internal sealed class ClaimsPrincipalBuilder
{
    private readonly Dictionary<string, string> _claims = [];

    private ClaimsPrincipalBuilder() { }

    public static ClaimsPrincipalBuilder From(IIdentity? identity = null)
    {
        var builder = new ClaimsPrincipalBuilder();
        if (identity != null) builder.WithIdentity(identity);
        return builder;
    }

    public static ClaimsPrincipalBuilder From(ClaimsPrincipal? identity = null)
    {
        var builder = new ClaimsPrincipalBuilder();
        if (identity != null) builder.WithIdentity(identity);
        return builder;
    }

    public ClaimsPrincipalBuilder WithIdentity(ClaimsPrincipal identity) => WithClaims(identity.Claims);

    public ClaimsPrincipalBuilder WithIdentity(IIdentity identity) =>
        identity is not ClaimsIdentity claimsIdentity
            ? throw new ArgumentException("Identity must be a ClaimsIdentity", nameof(identity))
            : WithClaims(claimsIdentity.Claims);

    public ClaimsPrincipalBuilder WithClaims(params IEnumerable<Claim> claims)
    {
        foreach (var claim in claims)
        {
            WithClaim(claim.Type, claim.Value);
        }
        return this;
    }

    public ClaimsPrincipalBuilder WithClaim(string type, string value)
    {
        if (type == ClaimsPrincipalExtensions.ScopeClaim)
        {
            return WithScope(value);
        }
        _claims[type] = value;
        return this;
    }

    public ClaimsPrincipalBuilder WithScope(string value)
    {
        _claims[ClaimsPrincipalExtensions.ScopeClaim] = _claims.TryGetValue(ClaimsPrincipalExtensions.ScopeClaim, out var existing)
            ? $"{existing} {value}"
            : value;
        return this;
    }

    public ClaimsPrincipalBuilder WithPid(string pid)
    {
        const string pidClaimType = "pid";
        _claims[pidClaimType] = pid;
        return this;
    }

    public ClaimsPrincipalBuilder Configure(Action<ClaimsPrincipalBuilder>? configure = null)
    {
        configure?.Invoke(this);
        return this;
    }

    public ClaimsPrincipal Build() =>
        new(new ClaimsIdentity(_claims
            .Select(x => new Claim(x.Key, x.Value))));

    public static implicit operator ClaimsPrincipal(ClaimsPrincipalBuilder builder)
    {
        return builder.Build();
    }
}
