using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
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
            return new Forbidden($"Use of transmission updates requires the scope {AuthorizationScope.ServiceProviderChangeTransmissions}.");
        }

        var dialog = await LoadDialogAsync(request.DialogId, cancellationToken);
        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var authorizeResult = await _serviceResourceAuthorizer.AuthorizeServiceResources(dialog, cancellationToken);
        if (authorizeResult.Value is Forbidden forbidden)
        {
            _domainContext.Pop();
            return forbidden;
        }

        if (dialog.Deleted)
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        if (dialog.Frozen && !_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            return new Forbidden("User cannot modify frozen dialog");
        }

        var transmission = dialog.Transmissions
            .FirstOrDefault(x => x.Id == request.TransmissionId);

        if (transmission is null)
        {
            return new EntityNotFound<DialogTransmission>(request.TransmissionId);
        }

        _mapper.Map(request.Dto, transmission);

        transmission.Attachments
            .Merge(request.Dto.Attachments,
                destinationKeySelector: x => x.Id,
                sourceKeySelector: x => x.Id,
                create: CreateTransmissionAttachments,
                update: UpdateTransmissionAttachments,
                delete: DeleteDelegate.Default);

        if (request.Dto.RelatedTransmissionId is not null &&
            request.Dto.RelatedTransmissionId.Value == request.TransmissionId)
        {
            _domainContext.AddError(new DomainFailure(nameof(UpdateTransmissionDto.RelatedTransmissionId),
                $"A transmission cannot reference itself ({nameof(UpdateTransmissionDto.RelatedTransmissionId)} is equal to {nameof(UpdateTransmissionDto.Id)}, '{request.TransmissionId}')."));
        }

        var conflict = await ValidateIdempotentKeys(dialog.Id, transmission, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        _transmissionHierarchyValidator.ValidateWholeAggregate(dialog);

        var saveResult = await _unitOfWork
            .DisableImmutableFilter()
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<UpdateTransmissionResult>(
            success => new UpdateTransmissionSuccess(dialog.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError,
            conflict => conflict);
    }

    private async Task<Conflict?> ValidateIdempotentKeys(Guid dialogId, DialogTransmission transmission, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transmission.IdempotentKey))
        {
            return null;
        }

        var exists = await _db.DialogTransmissions
            .Where(x => x.DialogId == dialogId && x.Id != transmission.Id)
            .AnyAsync(x => x.IdempotentKey == transmission.IdempotentKey, cancellationToken);

        return exists
            ? new Conflict(nameof(DialogTransmission.IdempotentKey),
                $"Duplicate IdempotentKey detected in dialog transmissions. Conflicting key: '{transmission.IdempotentKey}'.")
            : null;
    }

    private IEnumerable<DialogTransmissionAttachment> CreateTransmissionAttachments(IEnumerable<TransmissionAttachmentDto> creatables)
    {
        return creatables.Select(dto =>
        {
            var attachment = _mapper.Map<DialogTransmissionAttachment>(dto);
            attachment.EnsureId();
            attachment.Urls = _mapper.Map<List<AttachmentUrl>>(dto.Urls);
            _db.DialogTransmissionAttachments.Add(attachment);
            return attachment;
        });
    }

    private void UpdateTransmissionAttachments(IEnumerable<UpdateSet<DialogTransmissionAttachment, TransmissionAttachmentDto>> updateSets)
    {
        foreach (var (source, destination) in updateSets)
        {
            _mapper.Map(source, destination);
            destination.Urls = _mapper.Map<List<AttachmentUrl>>(source.Urls);
        }
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
                .ThenInclude(x => x.Content)
                    .ThenInclude(x => x.Value)
                        .ThenInclude(x => x.Localizations)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Attachments)
                    .ThenInclude(x => x.Urls)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Attachments)
                    .ThenInclude(x => x.DisplayName)
                        .ThenInclude(x => x!.Localizations)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.NavigationalActions)
                    .ThenInclude(x => x.Title)
                        .ThenInclude(x => x.Localizations)
            .Include(x => x.Transmissions)
                .ThenInclude(x => x.Sender)
            .IgnoreQueryFilters()
            .WhereIf(!isAdmin, x => x.Org == org)
            .FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);
    }
}
