using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;

public sealed class UpdateTransmissionCommand : IRequest<UpdateTransmissionResult>, ISilentUpdater, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid TransmissionId { get; set; }
    public Guid? IfMatchDialogRevision { get; set; }
    public UpdateTransmissionDto Dto { get; set; } = null!;
    public bool IsSilentUpdate { get; set; }

    Guid IFeatureMetricServiceResourceThroughDialogIdRequest.DialogId => DialogId;
}

[GenerateOneOf]
public sealed partial class UpdateTransmissionResult : OneOfBase<UpdateTransmissionSuccess, EntityNotFound, EntityDeleted, ValidationError, Forbidden, DomainError, ConcurrencyError, Conflict>;

public sealed record UpdateTransmissionSuccess(Guid Revision);

internal sealed class UpdateTransmissionCommandHandler : IRequestHandler<UpdateTransmissionCommand, UpdateTransmissionResult>
{
    private readonly IDialogDbContext _db;
    private readonly IDomainContext _domainContext;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceResourceAuthorizer _serviceResourceAuthorizer;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly ITransmissionHierarchyValidator _transmissionHierarchyValidator;

    public UpdateTransmissionCommandHandler(
        IDialogDbContext db,
        IDomainContext domainContext,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IServiceResourceAuthorizer serviceResourceAuthorizer,
        IUserResourceRegistry userResourceRegistry,
        ITransmissionHierarchyValidator transmissionHierarchyValidator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _serviceResourceAuthorizer = serviceResourceAuthorizer ?? throw new ArgumentNullException(nameof(serviceResourceAuthorizer));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _transmissionHierarchyValidator = transmissionHierarchyValidator ?? throw new ArgumentNullException(nameof(transmissionHierarchyValidator));
    }

    public async Task<UpdateTransmissionResult> Handle(UpdateTransmissionCommand request, CancellationToken cancellationToken)
    {
        if (!_userResourceRegistry.CurrentUserCanChangeTransmissions())
        {
            return new Forbidden($"Missing required scope '{AuthorizationScope.ServiceProviderChangeTransmissions}'.");
        }

        var dialog = await LoadDialogAsync(request.DialogId, cancellationToken);
        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        if (dialog.Frozen && !_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            return new Forbidden("User cannot modify frozen dialog");
        }

        var transmission = dialog.Transmissions.FirstOrDefault(x => x.Id == request.TransmissionId);
        if (transmission is null)
        {
            return new EntityNotFound<DialogTransmission>(request.TransmissionId);
        }

        await LoadTransmissionChildrenAsync(transmission, cancellationToken);
        ApplyUpdates(transmission, request.Dto);

        _transmissionHierarchyValidator.ValidateWholeAggregate(dialog);

        var authorizeResult = await _serviceResourceAuthorizer.AuthorizeServiceResources(dialog, cancellationToken);
        if (authorizeResult.Value is Forbidden forbidden)
        {
            _domainContext.Pop();
            return forbidden;
        }

        var saveResult = await _unitOfWork
            .DisableAggregateFilter()
            .DisableImmutableFilter()
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<UpdateTransmissionResult>(
            success => new UpdateTransmissionSuccess(dialog.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError,
            conflict => conflict);
    }

    private async Task<DialogEntity?> LoadDialogAsync(Guid dialogId, CancellationToken cancellationToken)
    {
        var isAdmin = _userResourceRegistry.IsCurrentUserServiceOwnerAdmin();
        var org = string.Empty;
        if (!isAdmin)
        {
            org = await _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
        }

        return await _db.Dialogs
            .Include(x => x.Transmissions)
            .IgnoreQueryFilters()
            .WhereIf(!isAdmin, x => x.Org == org)
            .FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);
    }

    private async Task LoadTransmissionChildrenAsync(DialogTransmission transmission, CancellationToken cancellationToken)
    {
        await _db.Entry(transmission)
            .Collection(x => x.Content)
            .Query()
            .Include(x => x.Value.Localizations)
            .LoadAsync(cancellationToken);

        await _db.Entry(transmission)
            .Collection(x => x.Attachments)
            .Query()
            .Include(x => x.DisplayName)
                .ThenInclude(x => x!.Localizations)
            .Include(x => x.Urls)
            .LoadAsync(cancellationToken);

        await _db.Entry(transmission)
            .Collection(x => x.NavigationalActions)
            .Query()
            .Include(x => x.Title)
                .ThenInclude(x => x.Localizations)
            .LoadAsync(cancellationToken);

        await _db.Entry(transmission)
            .Reference(x => x.Sender)
            .Query()
            .Include(x => x.ActorNameEntity)
            .LoadAsync(cancellationToken);
    }

    private void ApplyUpdates(DialogTransmission transmission, UpdateTransmissionDto dto)
    {
        transmission.CreatedAt = dto.CreatedAt;
        transmission.AuthorizationAttribute = dto.AuthorizationAttribute;
        transmission.ExtendedType = dto.ExtendedType;
        transmission.ExternalReference = dto.ExternalReference;
        transmission.RelatedTransmissionId = dto.RelatedTransmissionId;
        transmission.TypeId = dto.Type;

        transmission.Sender = _mapper.Map<DialogTransmissionSenderActor>(dto.Sender);

        var content = _mapper.Map<List<DialogTransmissionContent>?>(dto.Content) ?? [];
        transmission.Content.Clear();
        transmission.Content.AddRange(content);

        var attachments = _mapper.Map<List<DialogTransmissionAttachment>>(dto.Attachments);
        transmission.Attachments.Clear();
        transmission.Attachments.AddRange(attachments);

        var navigationalActions = _mapper.Map<List<DialogTransmissionNavigationalAction>>(dto.NavigationalActions);
        transmission.NavigationalActions.Clear();
        transmission.NavigationalActions.AddRange(navigationalActions);
    }
}
