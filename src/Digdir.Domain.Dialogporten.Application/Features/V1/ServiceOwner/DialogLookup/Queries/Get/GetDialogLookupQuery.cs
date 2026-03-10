using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using FluentValidation.Results;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogLookup.Queries.Get;

public sealed class GetDialogLookupQuery : IRequest<GetDialogLookupResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public string InstanceRef { get; set; } = null!;
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

[GenerateOneOf]
public sealed partial class GetDialogLookupResult : OneOfBase<ServiceOwnerIdentifierLookupDto, EntityNotFound, Forbidden, ValidationError>;

internal sealed class GetDialogLookupQueryHandler : IRequestHandler<GetDialogLookupQuery, GetDialogLookupResult>
{
    private readonly IIdentifierLookupDialogResolver _dialogResolver;
    private readonly IIdentifierLookupPresentationResolver _presentationResolver;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public GetDialogLookupQueryHandler(
        IIdentifierLookupDialogResolver dialogResolver,
        IIdentifierLookupPresentationResolver presentationResolver,
        IUserResourceRegistry userResourceRegistry)
    {
        _dialogResolver = dialogResolver ?? throw new ArgumentNullException(nameof(dialogResolver));
        _presentationResolver = presentationResolver ?? throw new ArgumentNullException(nameof(presentationResolver));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
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
            IdentifierLookupDeletedDialogVisibility.IncludeDeleted,
            cancellationToken);
        if (dialogData is null)
        {
            return new EntityNotFound(nameof(request.InstanceRef), [request.InstanceRef]);
        }

        if (!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            var callerOrg = await _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
            if (!string.Equals(callerOrg, dialogData.Org, StringComparison.OrdinalIgnoreCase))
            {
                return new Forbidden("The authenticated service owner does not own the resolved dialog.");
            }
        }

        var responseInstanceRef = _dialogResolver.ResolveOutputInstanceRef(instanceRef, dialogData);

        var (serviceResource, serviceOwner) = await _presentationResolver.Resolve(
            dialogData.ServiceResource,
            dialogData.Org,
            request.AcceptedLanguages,
            cancellationToken);

        var title = ToLocalizations(dialogData.Title);
        title.PruneLocalizations(request.AcceptedLanguages);

        var nonSensitiveTitle = dialogData.NonSensitiveTitle is null
            ? null
            : ToLocalizations(dialogData.NonSensitiveTitle);
        nonSensitiveTitle?.PruneLocalizations(request.AcceptedLanguages);

        return new ServiceOwnerIdentifierLookupDto
        {
            DialogId = dialogData.DialogId,
            InstanceRef = responseInstanceRef,
            ServiceResource = serviceResource,
            ServiceOwner = serviceOwner,
            Title = title,
            NonSensitiveTitle = nonSensitiveTitle
        };
    }

    private static List<LocalizationDto> ToLocalizations(IEnumerable<ResourceLocalization> localizations) =>
        localizations
            .Select(x => new LocalizationDto
            {
                LanguageCode = x.LanguageCode,
                Value = x.Value
            })
            .ToList();
}
