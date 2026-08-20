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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceResourceAuthorizer _serviceResourceAuthorizer;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly ITransmissionHierarchyValidator _transmissionHierarchyValidator;

    public UpdateTransmissionCommandHandler(
        IDialogDbContext db,
        IUnitOfWork unitOfWork,
        IServiceResourceAuthorizer serviceResourceAuthorizer,
        IUserResourceRegistry userResourceRegistry,
        ITransmissionHierarchyValidator transmissionHierarchyValidator)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(serviceResourceAuthorizer);
        ArgumentNullException.ThrowIfNull(userResourceRegistry);
        ArgumentNullException.ThrowIfNull(transmissionHierarchyValidator);

        _db = db;
        _unitOfWork = unitOfWork;
        _serviceResourceAuthorizer = serviceResourceAuthorizer;
        _userResourceRegistry = userResourceRegistry;
        _transmissionHierarchyValidator = transmissionHierarchyValidator;
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

        transmission.UpdateFrom(request.Dto);

        transmission.Attachments
            .Merge(request.Dto.Attachments,
                destinationKeySelector: x => x.Id,
                sourceKeySelector: x => x.Id,
                create: CreateTransmissionAttachments,
                update: UpdateTransmissionAttachments,
                delete: DeleteDelegate.Default);

        // Authorization of referenced service resources must happen after the incoming DTO has been
        // mapped onto the aggregate, so that incoming authorization attributes are covered.
        var authorizeResult = await _serviceResourceAuthorizer.AuthorizeServiceResources(dialog, cancellationToken);
        if (authorizeResult.Value is Forbidden forbidden)
        {
            return forbidden;
        }

        var conflict = ValidateIdempotentKeys(dialog, transmission);
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

    private static Conflict? ValidateIdempotentKeys(DialogEntity dialog, DialogTransmission transmission)
    {
        if (string.IsNullOrWhiteSpace(transmission.IdempotentKey))
        {
            return null;
        }

        var exists = dialog.Transmissions
            .Where(x => x.Id != transmission.Id)
            .Any(x => x.IdempotentKey == transmission.IdempotentKey);

        return exists
            ? new Conflict(nameof(DialogTransmission.IdempotentKey),
                $"Duplicate IdempotentKey detected in dialog transmissions. Conflicting key: '{transmission.IdempotentKey}'.")
            : null;
    }

    private IEnumerable<DialogTransmissionAttachment> CreateTransmissionAttachments(IEnumerable<TransmissionAttachmentDto> creatables)
    {
        return creatables.Select(dto =>
        {
            var attachment = dto.ToDialogTransmissionAttachment();
            attachment.EnsureId();
            _db.DialogTransmissionAttachments.Add(attachment);
            return attachment;
        });
    }

    private void UpdateTransmissionAttachments(IEnumerable<UpdateSet<DialogTransmissionAttachment, TransmissionAttachmentDto>> updateSets)
    {
        foreach (var (source, destination) in updateSets)
        {
            destination.UpdateFrom(source);
            destination.Urls
                .Merge(source.Urls,
                    destinationKeySelector: x => x.Id,
                    sourceKeySelector: x => x.Id,
                    create: CreateAttachmentUrls,
                    update: UpdateAttachmentUrls,
                    delete: DeleteDelegate.Default);
        }
    }

    private static void UpdateAttachmentUrls(IEnumerable<UpdateSet<AttachmentUrl, TransmissionAttachmentUrlDto>> updateSets)
    {
        foreach (var (source, destination) in updateSets)
        {
            destination.UpdateFrom(source);
        }
    }

    private IEnumerable<AttachmentUrl> CreateAttachmentUrls(IEnumerable<TransmissionAttachmentUrlDto> creatables)
    {
        foreach (var dto in creatables)
        {
            var url = dto.ToAttachmentUrl();
            _db.AttachmentUrls.Add(url);
            yield return url;
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
