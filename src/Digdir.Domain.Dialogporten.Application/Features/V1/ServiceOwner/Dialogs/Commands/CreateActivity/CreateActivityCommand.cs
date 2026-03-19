using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.SystemLabelAdder;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using ResourceRegistryConstants = Digdir.Domain.Dialogporten.Application.Common.ResourceRegistry.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

public sealed class CreateActivityCommand : IRequest<CreateActivityResult>,
    ISilentUpdater,
    IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }

    public Guid? IfMatchDialogRevision { get; set; }

    public required CreateActivityDto Activity { get; set; }

    public bool IsSilentUpdate { get; set; }
}

[GenerateOneOf]
public sealed partial class CreateActivityResult : OneOfBase<
    CreateActivitySuccess,
    EntityNotFound,
    EntityDeleted,
    ValidationError,
    Forbidden,
    DomainError,
    ConcurrencyError,
    Conflict
>;

public sealed record CreateActivitySuccess(Guid Revision, Guid ActivityId);

internal sealed class CreateActivityCommandHandler : IRequestHandler<CreateActivityCommand, CreateActivityResult>
{
    private readonly IDomainContext _domainContext;
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _user;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IServiceResourceAuthorizer _serviceResourceAuthorizer;
    private readonly IMapper _mapper;
    private readonly ISystemLabelAdder _systemLabelAdder;

    public CreateActivityCommandHandler(
        IDomainContext domainContext,
        IUnitOfWork unitOfWork,
        IDialogDbContext db,
        IUser user,
        IUserResourceRegistry userResourceRegistry,
        IServiceResourceAuthorizer serviceResourceAuthorizer,
        IMapper mapper,
        ISystemLabelAdder systemLabelAdder)
    {
        _domainContext = domainContext;
        _unitOfWork = unitOfWork;
        _db = db;
        _user = user;
        _userResourceRegistry = userResourceRegistry;
        _mapper = mapper;
        _systemLabelAdder = systemLabelAdder;
        _serviceResourceAuthorizer = serviceResourceAuthorizer;
    }

    public async Task<CreateActivityResult> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        var dialog = await LoadDialogAsync(request.DialogId, cancellationToken);
        if (dialog == null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        if (dialog.Deleted && !_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            return new EntityDeleted<DialogEntity>(request.DialogId);
        }

        if (dialog.Frozen && !_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            return new Forbidden("User cannot modify frozen dialog");
        }

        var newActivity = _mapper.Map<DialogActivity>(request.Activity);
        newActivity.Id = newActivity.EnsureId();
        newActivity.DialogId = dialog.Id;

        var activityTypeForbidden = ValidateActivityTypeAuthorization(newActivity);
        if (activityTypeForbidden is not null)
        {
            return activityTypeForbidden;
        }

        var transmissionRelationError =
            await ValidateActivityTransmissionRelationAsync(dialog.Id, newActivity, cancellationToken);
        if (transmissionRelationError is not null)
        {
            return transmissionRelationError;
        }

        await UpdateHasUnopenedContentAsync(dialog, newActivity, cancellationToken);

        _db.DialogActivities.Add(newActivity);

        var authorizeResult = await _serviceResourceAuthorizer.AuthorizeServiceResources(dialog, cancellationToken);
        if (authorizeResult.Value is Forbidden forbidden)
        {
            _domainContext.Pop();
            return forbidden;
        }

        if (!request.IsSilentUpdate)
        {
            _systemLabelAdder.AddSystemLabel(dialog, SystemLabel.Values.Default);
        }

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<CreateActivityResult>(
            success => new CreateActivitySuccess(dialog.Revision, newActivity.Id),
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
            .Include(x => x.EndUserContext)
            .ThenInclude(x => x.DialogEndUserContextSystemLabels)
            .IgnoreQueryFilters()
            .WhereIf(!isAdmin, x => x.Org == org)
            .FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);
    }

    private Forbidden? ValidateActivityTypeAuthorization(DialogActivity newActivity)
    {
        IEnumerable<DialogActivityType.Values> activityTypes = [newActivity.TypeId];

        if (!ActivityTypeAuthorization.UsingAllowedActivityTypes(activityTypes, _user, out var errorMessage))
        {
            return new Forbidden(errorMessage);
        }

        return null;
    }

    private async Task<DomainError?> ValidateActivityTransmissionRelationAsync(Guid dialogId, DialogActivity newActivity,
        CancellationToken cancellationToken)
    {
        if (newActivity.TransmissionId is not { } transmissionId)
        {
            return null;
        }

        var transmissionExists = await _db.DialogTransmissions
            .Where(x => x.DialogId == dialogId)
            .AnyAsync(x => x.Id == transmissionId, cancellationToken);

        if (transmissionExists)
        {
            return null;
        }

        return new DomainError(new DomainFailure(
            $"{nameof(CreateActivityCommand.Activity)}.{nameof(CreateActivityDto.TransmissionId)}",
            $"Invalid '{nameof(DialogActivity.TransmissionId)}', entity '{nameof(DialogTransmission)}' with the following key(s) does not exist: ({transmissionId}) in '{nameof(DialogEntity.Transmissions)}'"));
    }

    private async Task UpdateHasUnopenedContentAsync(DialogEntity dialog, DialogActivity newActivity,
        CancellationToken cancellationToken)
    {
        if (!dialog.HasUnopenedContent)
        {
            return;
        }

        if (newActivity.TypeId == DialogActivityType.Values.TransmissionOpened)
        {
            await UpdateHasUnopenedContentForTransmissionOpenedAsync(dialog, newActivity, cancellationToken);
            return;
        }

        if (newActivity.TypeId == DialogActivityType.Values.CorrespondenceOpened)
        {
            await UpdateHasUnopenedContentForCorrespondenceOpenedAsync(dialog.Id, dialog, cancellationToken);
        }
    }

    private async Task UpdateHasUnopenedContentForTransmissionOpenedAsync(DialogEntity dialog, DialogActivity newActivity,
        CancellationToken cancellationToken)
    {
        if (newActivity.TransmissionId is not { } transmissionId)
        {
            return;
        }

        var transmissionType = await _db.DialogTransmissions
            .Where(x => x.DialogId == dialog.Id && x.Id == transmissionId)
            .Select(x => (DialogTransmissionType.Values?)x.TypeId)
            .SingleOrDefaultAsync(cancellationToken);

        if (transmissionType is not { } type || !DialogUnopenedContent.IsRelevantTransmissionType(type))
        {
            return;
        }

        var hasOtherUnopenedRelevantTransmissions =
            await HasUnopenedRelevantTransmissionsAsync(dialog.Id, transmissionId, cancellationToken);

        if (hasOtherUnopenedRelevantTransmissions)
        {
            return;
        }

        if (dialog.ServiceResourceType == ResourceRegistryConstants.CorrespondenceService)
        {
            var hasCorrespondenceOpened = await _db.DialogActivities
                .Where(x => x.DialogId == dialog.Id)
                .AnyAsync(x => x.TypeId == DialogActivityType.Values.CorrespondenceOpened, cancellationToken);

            if (!hasCorrespondenceOpened)
            {
                return;
            }
        }

        dialog.HasUnopenedContent = false;
    }

    private async Task UpdateHasUnopenedContentForCorrespondenceOpenedAsync(Guid dialogId, DialogEntity dialog,
        CancellationToken cancellationToken)
    {
        if (dialog.ServiceResourceType != ResourceRegistryConstants.CorrespondenceService)
        {
            return;
        }

        var hasUnopenedRelevantTransmissions =
            await HasUnopenedRelevantTransmissionsAsync(dialogId, transmissionIdToExclude: null, cancellationToken);

        if (!hasUnopenedRelevantTransmissions)
        {
            dialog.HasUnopenedContent = false;
        }
    }

    private Task<bool> HasUnopenedRelevantTransmissionsAsync(Guid dialogId, Guid? transmissionIdToExclude,
        CancellationToken cancellationToken)
    {
        var query = _db.DialogTransmissions
            .Where(x => x.DialogId == dialogId)
            .Where(x => x.TypeId != DialogTransmissionType.Values.Correction &&
                x.TypeId != DialogTransmissionType.Values.Submission);

        if (transmissionIdToExclude is { } transmissionId)
        {
            query = query.Where(x => x.Id != transmissionId);
        }

        return query.AnyAsync(x => x.Activities
            .All(a => a.TypeId != DialogActivityType.Values.TransmissionOpened), cancellationToken);
    }
}
