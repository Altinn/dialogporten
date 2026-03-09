using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogLookup.Queries.Get;

public sealed class GetDialogLookupQuery : IRequest<GetDialogLookupResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public string InstanceUrn { get; set; } = null!;
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

[GenerateOneOf]
public sealed partial class GetDialogLookupResult : OneOfBase<EndUserIdentifierLookupDto, EntityNotFound, Forbidden, ValidationError>;

internal sealed class GetDialogLookupQueryValidator : AbstractValidator<GetDialogLookupQuery>
{
    public GetDialogLookupQueryValidator()
    {
        RuleFor(x => x.InstanceUrn)
            .Must(value => InstanceUrn.TryParse(value, out _))
            .WithMessage("InstanceUrn must be a supported URN: 'urn:altinn:app-instance-id:{uuid}', 'urn:altinn:correspondence-id:{uuid}' or 'urn:altinn:dialog-id:{uuid}'.");
    }
}

internal sealed class GetDialogLookupQueryHandler : IRequestHandler<GetDialogLookupQuery, GetDialogLookupResult>
{
    private readonly IIdentifierLookupDialogResolver _dialogResolver;
    private readonly IIdentifierLookupPresentationResolver _presentationResolver;
    private readonly IIdentifierLookupAuthorizationResolver _authorizationResolver;

    public GetDialogLookupQueryHandler(
        IIdentifierLookupDialogResolver dialogResolver,
        IIdentifierLookupPresentationResolver presentationResolver,
        IIdentifierLookupAuthorizationResolver authorizationResolver)
    {
        _dialogResolver = dialogResolver ?? throw new ArgumentNullException(nameof(dialogResolver));
        _presentationResolver = presentationResolver ?? throw new ArgumentNullException(nameof(presentationResolver));
        _authorizationResolver = authorizationResolver ?? throw new ArgumentNullException(nameof(authorizationResolver));
    }

    public async Task<GetDialogLookupResult> Handle(GetDialogLookupQuery request, CancellationToken cancellationToken)
    {
        if (!InstanceUrn.TryParse(request.InstanceUrn, out var urn))
        {
            return new ValidationError(new ValidationFailure(
                nameof(request.InstanceUrn),
                "InstanceUrn must be a supported URN: 'urn:altinn:app-instance-id:{uuid}', 'urn:altinn:correspondence-id:{uuid}' or 'urn:altinn:dialog-id:{uuid}'."));
        }

        var dialogData = await _dialogResolver.Resolve(
            urn,
            IdentifierLookupDeletedDialogVisibility.ExcludeDeleted,
            cancellationToken);
        if (dialogData is null)
        {
            return new EntityNotFound(nameof(request.InstanceUrn), [request.InstanceUrn]);
        }

        var responseInstanceUrn = _dialogResolver.ResolveOutputInstanceUrn(urn, dialogData);

        var authorization = await _authorizationResolver.Resolve(
            dialogData,
            urn,
            responseInstanceUrn,
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
            InstanceUrn = responseInstanceUrn,
            ServiceResource = serviceResource,
            ServiceOwner = serviceOwner,
            AuthorizationEvidence = authorization.Evidence
        };
    }
}
