using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed class TestAltinnAuthorization
{
    public IAltinnAuthorization? Override { get; private set; }

    public void Configure(Action<IAltinnAuthorization> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var substitute = Substitute.For<IAltinnAuthorization>();
        configure(substitute);
        Override = substitute;
    }

    public void Reset() => Override = null;
}

internal sealed class RoutedAltinnAuthorization : IAltinnAuthorization
{
    private readonly TestAltinnAuthorization _testAltinnAuthorization;
    private readonly LocalDevelopmentAltinnAuthorization _fallbackAuthorization;

    public RoutedAltinnAuthorization(
        TestAltinnAuthorization testAltinnAuthorization,
        LocalDevelopmentAltinnAuthorization fallbackAuthorization)
    {
        ArgumentNullException.ThrowIfNull(testAltinnAuthorization);
        ArgumentNullException.ThrowIfNull(fallbackAuthorization);

        _testAltinnAuthorization = testAltinnAuthorization;
        _fallbackAuthorization = fallbackAuthorization;
    }

    private IAltinnAuthorization Current => _testAltinnAuthorization.Override ?? _fallbackAuthorization;

    public Task<DialogDetailsAuthorizationResult> GetDialogDetailsAuthorization(
        DialogEntity dialogEntity,
        CancellationToken cancellationToken = default) =>
        Current.GetDialogDetailsAuthorization(dialogEntity, cancellationToken);

    public Task<DialogSearchAuthorizationResult> GetAuthorizedResourcesForSearch(
        List<string> constraintParties,
        List<string> constraintServiceResources,
        CancellationToken cancellationToken = default) =>
        Current.GetAuthorizedResourcesForSearch(constraintParties, constraintServiceResources, cancellationToken);

    public Task<AuthorizedPartiesResult> GetAuthorizedParties(
        IPartyIdentifier authenticatedParty,
        bool flatten = false,
        CancellationToken cancellationToken = default) =>
        Current.GetAuthorizedParties(authenticatedParty, flatten, cancellationToken);

    public Task<AuthorizedPartiesResult> GetAuthorizedPartiesForLookup(
        IPartyIdentifier authenticatedParty,
        List<string> constraintParties,
        CancellationToken cancellationToken = default) =>
        Current.GetAuthorizedPartiesForLookup(authenticatedParty, constraintParties, cancellationToken);

    public Task<bool> HasListAuthorizationForDialog(DialogEntity dialog, CancellationToken cancellationToken) =>
        Current.HasListAuthorizationForDialog(dialog, cancellationToken);

    public bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel) =>
        Current.UserHasRequiredAuthLevel(minimumAuthenticationLevel);

    public Task<bool> UserHasRequiredAuthLevel(string serviceResource, CancellationToken cancellationToken) =>
        Current.UserHasRequiredAuthLevel(serviceResource, cancellationToken);
}

internal static class TestAltinnAuthorizationExtensions
{
    extension<TFlowStep>(TFlowStep flowStep) where TFlowStep : IFlowStep
    {
        public TFlowStep ConfigureAltinnAuthorization(Action<IAltinnAuthorization> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            return flowStep.Do(_ => DialogApplication.AltinnAuthorization.Configure(configure));
        }
    }
}
