using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogLookup.Queries.Get;

public sealed class GetDialogLookupQuery : IRequest<GetDialogLookupResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public string InstanceRef { get; set; } = null!;
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

[GenerateOneOf]
public sealed partial class GetDialogLookupResult : OneOfBase<EndUserIdentifierLookupDto, EntityNotFound, Forbidden, ValidationError>;

internal sealed class GetDialogLookupQueryHandler : IRequestHandler<GetDialogLookupQuery, GetDialogLookupResult>
{
    private readonly IIdentifierLookupDialogResolver _dialogResolver;
    private readonly IIdentifierLookupPresentationResolver _presentationResolver;
    private readonly IIdentifierLookupAuthorizationResolver _authorizationResolver;
    private readonly ILogger<GetDialogLookupQueryHandler> _logger;

    public GetDialogLookupQueryHandler(
        IIdentifierLookupDialogResolver dialogResolver,
        IIdentifierLookupPresentationResolver presentationResolver,
        IIdentifierLookupAuthorizationResolver authorizationResolver,
        ILogger<GetDialogLookupQueryHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(dialogResolver);
        ArgumentNullException.ThrowIfNull(presentationResolver);
        ArgumentNullException.ThrowIfNull(authorizationResolver);
        ArgumentNullException.ThrowIfNull(logger);

        _dialogResolver = dialogResolver;
        _presentationResolver = presentationResolver;
        _authorizationResolver = authorizationResolver;
        _logger = logger;
    }

    public async Task<GetDialogLookupResult> Handle(GetDialogLookupQuery request, CancellationToken cancellationToken)
    {
        if (!InstanceRef.TryParse(request.InstanceRef, out var parsedInstanceRef))
        {
            return new ValidationError(new ValidationFailure(
                nameof(request.InstanceRef),
                "InstanceRef must be a supported identifier: 'urn:altinn:instance-id:{partyId}/{uuid}', 'urn:altinn:correspondence-id:{uuid}' or 'urn:altinn:dialog-id:{uuid}'."));
        }

        var instanceRef = parsedInstanceRef.Value;

        var dialogData = await _dialogResolver.Resolve(
            instanceRef,
            IdentifierLookupDeletedDialogVisibility.ExcludeDeleted,
            cancellationToken);
        if (dialogData is null)
        {
            return new EntityNotFound(nameof(request.InstanceRef), [request.InstanceRef]);
        }

        var responseInstanceRef = _dialogResolver.ResolveOutputInstanceRef(instanceRef, dialogData);
        if (!InstanceRef.TryParse(responseInstanceRef, out var parsedResponseInstanceRef))
        {
            _logger.LogError(
                "Identifier lookup resolved an invalid response instance reference. RequestInstanceRef={RequestInstanceRef}, ResolvedResponseInstanceRef={ResolvedResponseInstanceRef}, DialogId={DialogId}, ServiceResource={ServiceResource}",
                request.InstanceRef,
                responseInstanceRef,
                dialogData.DialogId,
                dialogData.ServiceResource);

            throw new UnreachableException("Resolved response instance reference is invalid.");
        }

        var authorization = await _authorizationResolver.Resolve(
            dialogData,
            instanceRef,
            parsedResponseInstanceRef.Value,
            cancellationToken);

        if (!authorization.HasAccess)
        {
            return new Forbidden("Forbidden");
        }

        var (serviceResource, serviceOwner) = await _presentationResolver.Resolve(
            dialogData.ServiceResource,
            dialogData.Org,
            request.AcceptedLanguages,
            cancellationToken);

        return new EndUserIdentifierLookupDto
        {
            DialogId = dialogData.DialogId,
            InstanceRef = responseInstanceRef,
            ServiceResource = serviceResource,
            ServiceOwner = serviceOwner,
            AuthorizationEvidence = authorization.Evidence
        };
    }
}
