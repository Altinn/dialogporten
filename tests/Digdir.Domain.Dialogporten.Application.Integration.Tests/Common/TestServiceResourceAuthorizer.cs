using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed class TestServiceResourceAuthorizer : IServiceResourceAuthorizer
{
    private Func<DialogEntity, AuthorizeServiceResourcesResult>? _authorizeOverride;

    public void OverrideAuthorization(Func<DialogEntity, AuthorizeServiceResourcesResult> authorize)
    {
        ArgumentNullException.ThrowIfNull(authorize);

        _authorizeOverride = authorize;
    }

    public void Reset() => _authorizeOverride = null;

    public Task<AuthorizeServiceResourcesResult> AuthorizeServiceResources(DialogEntity dialog,
        CancellationToken cancellationToken)
    {
        AuthorizeServiceResourcesResult authorized = new Success();
        return Task.FromResult(_authorizeOverride?.Invoke(dialog) ?? authorized);
    }

    public Task<SetResourceTypeResult> SetResourceType(DialogEntity dialog, CancellationToken cancellationToken)
    {
        dialog.ServiceResourceType = "GenericAccessResource";
        return Task.FromResult<SetResourceTypeResult>(new Success());
    }
}

internal static class TestServiceResourceAuthorizerExtensions
{
    extension<TFlowStep>(TFlowStep flowStep) where TFlowStep : IFlowStep
    {
        /// <summary>
        /// Emulates a PDP verdict on the service resources referenced by the dialog aggregate, letting a
        /// test deny resources the default authorizer would allow.
        /// </summary>
        public TFlowStep OverrideServiceResourceAuthorization(
            Func<DialogEntity, AuthorizeServiceResourcesResult> authorize)
        {
            ArgumentNullException.ThrowIfNull(authorize);

            return flowStep.Do(_ => DialogApplication.ServiceResourceAuthorizer.OverrideAuthorization(authorize));
        }
    }
}
