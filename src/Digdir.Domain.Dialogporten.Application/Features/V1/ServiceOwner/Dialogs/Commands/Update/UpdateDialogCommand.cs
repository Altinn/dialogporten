using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

public sealed class UpdateDialogCommand : IRequest<UpdateDialogResult>, ISilentUpdater, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid Id { get; set; }
    public Guid? IfMatchDialogRevision { get; set; }
    public UpdateDialogDto Dto { get; set; } = null!;
    public bool IsSilentUpdate { get; set; }

    Guid IFeatureMetricServiceResourceThroughDialogIdRequest.DialogId => Id;
}

[GenerateOneOf]
public sealed partial class UpdateDialogResult : OneOfBase<UpdateDialogSuccess, EntityNotFound, EntityDeleted, ValidationError, Forbidden, DomainError, ConcurrencyError>;

public sealed record UpdateDialogSuccess(Guid Revision);

internal sealed class UpdateDialogCommandHandler : IRequestHandler<UpdateDialogCommand, UpdateDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUser _user;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainContext _domainContext;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IServiceResourceAuthorizer _serviceResourceAuthorizer;
    private readonly IDataLoaderContext _dataLoaderContext;
    private readonly IDialogTransmissionAppender _dialogTransmissionAppender;
    private readonly ITransmissionHierarchyValidator _transmissionHierarchyValidator;

    public UpdateDialogCommandHandler(
        IDialogDbContext db,
        IUser user,
        IClock clock,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IDomainContext domainContext,
        IUserResourceRegistry userResourceRegistry,
        IResourceRegistry resourceRegistry,
        IServiceResourceAuthorizer serviceResourceAuthorizer,
        IDataLoaderContext dataLoaderContext,
        IDialogTransmissionAppender dialogTransmissionAppender,
        ITransmissionHierarchyValidator transmissionHierarchyValidator)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _serviceResourceAuthorizer = serviceResourceAuthorizer ?? throw new ArgumentNullException(nameof(serviceResourceAuthorizer));
        _dataLoaderContext = dataLoaderContext ?? throw new ArgumentNullException(nameof(dataLoaderContext));
        _dialogTransmissionAppender = dialogTransmissionAppender ?? throw new ArgumentNullException(nameof(dialogTransmissionAppender));
        _transmissionHierarchyValidator = transmissionHierarchyValidator ?? throw new ArgumentNullException(nameof(transmissionHierarchyValidator));
    }

    public async Task<UpdateDialogResult> Handle(UpdateDialogCommand request, CancellationToken cancellationToken)
    {
        var dialog = UpdateDialogDataLoader.GetPreloadedData(_dataLoaderContext);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.Id);
        }

        if (dialog.Deleted)
        {
            // TODO: https://github.com/altinn/dialogporten/issues/1543
            // When restoration is implemented, add a hint to the error message.
            return new EntityDeleted<DialogEntity>(request.Id);
        }

        var isCurrentUserServiceOwnerAdmin = _userResourceRegistry.IsCurrentUserServiceOwnerAdmin();
        if (dialog.Frozen && !isCurrentUserServiceOwnerAdmin)
        {
            return new Forbidden("User cannot modify frozen dialog");
        }

        // Update primitive properties
        _mapper.Map(request.Dto, dialog);

        if (!request.IsSilentUpdate || !isCurrentUserServiceOwnerAdmin)
        {
            ValidateTimeFields(dialog);
        }

        AppendActivity(dialog, request.Dto);

        var activityTypes = dialog.Activities
            .Select(x => x.TypeId)
            .Distinct();

        if (!ActivityTypeAuthorization.UsingAllowedActivityTypes(activityTypes, _user, out var errorMessage))
        {
            return new Forbidden(errorMessage);
        }

        await AppendTransmission(dialog, request.Dto, cancellationToken);

        _domainContext.AddErrors(dialog.Transmissions.ValidateReferenceHierarchy(
            keySelector: x => x.Id,
            parentKeySelector: x => x.RelatedTransmissionId,
            propertyName: nameof(UpdateDialogDto.Transmissions),
            maxDepth: 20,
            maxWidth: 20));

        VerifyActivityTransmissionRelations(dialog);

        dialog.SearchTags
            .Merge(request.Dto.SearchTags,
                destinationKeySelector: x => x.Value,
                sourceKeySelector: x => x.Value,
                create: _mapper.Map<List<DialogSearchTag>>,
                delete: DeleteDelegate.Default,
                comparer: StringComparer.InvariantCultureIgnoreCase);

        dialog.Attachments
            .Merge(request.Dto.Attachments,
                destinationKeySelector: x => x.Id,
                sourceKeySelector: x => x.Id,
                create: CreateAttachments,
                update: UpdateAttachments,
                delete: DeleteDelegate.Default);

        dialog.GuiActions
            .Merge(request.Dto.GuiActions,
                destinationKeySelector: x => x.Id,
                sourceKeySelector: x => x.Id,
                create: CreateGuiActions,
                update: _mapper.Update,
                delete: DeleteDelegate.Default);

        dialog.ApiActions
            .Merge(request.Dto.ApiActions,
                destinationKeySelector: x => x.Id,
                sourceKeySelector: x => x.Id,
                create: CreateApiActions,
                update: UpdateApiActions,
                delete: DeleteDelegate.Default);

        var serviceResourceAuthorizationResult = await _serviceResourceAuthorizer.AuthorizeServiceResources(dialog, cancellationToken);
        if (serviceResourceAuthorizationResult.Value is Forbidden forbiddenResult)
        {
            // Ignore the domain context errors, as they are not relevant when returning Forbidden.
            _domainContext.Pop();
            return forbiddenResult;
        }

        var serviceResourceInformation = await _resourceRegistry.GetResourceInformation(dialog.ServiceResource, cancellationToken);
        dialog.HasUnopenedContent = DialogUnopenedContent.HasUnopenedContent(dialog, serviceResourceInformation);

        if (!request.IsSilentUpdate)
        {
            AddSystemLabel(dialog, SystemLabel.Values.Default);
        }

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<UpdateDialogResult>(
            success => new UpdateDialogSuccess(dialog.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }

    private void AddSystemLabel(DialogEntity dialog, SystemLabel.Values labelToAdd)
    {
        if (!_user.GetPrincipal().TryGetConsumerOrgNumber(out var organizationNumber))
        {
            _domainContext.AddError(new DomainFailure(nameof(organizationNumber), "Cannot find organization number for current user."));
            return;
        }

        var performedBy = LabelAssignmentLogActorFactory.Create(
            ActorType.Values.ServiceOwner,
            actorId: $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}{organizationNumber}",
            actorName: null);

        dialog.EndUserContext.UpdateSystemLabels(
            addLabels: [labelToAdd],
            removeLabels: [],
            performedBy);
    }

    private void ValidateTimeFields(DialogAttachment attachment)
    {
        if (!_db.MustWhenModified(attachment,
            propertyExpression: x => x.ExpiresAt,
            predicate: x => x > _clock.UtcNowOffset || x == null))
        {
            _domainContext.AddError($"{nameof(UpdateDialogDto.Attachments)}." +
                $"{nameof(AttachmentDto.ExpiresAt)}",
                $"Must be in future, current value, or null. (Id: {attachment.Id})");
            return;
        }

        if (!_db.MustWhenAdded(attachment,
            propertyExpression: x => x.ExpiresAt,
            predicate: x => x > _clock.UtcNowOffset || x == null))
        {
            var idString = attachment.Id == Guid.Empty ? string.Empty : $" (Id: {attachment.Id})";
            _domainContext.AddError($"{nameof(UpdateDialogDto.Attachments)}." +
                $"{nameof(AttachmentDto.ExpiresAt)}",
                $"Must be in future or null, got '{attachment.ExpiresAt}'.{idString}");
        }
    }

    private void ValidateTimeFields(DialogEntity dialog)
    {
        const string errorMessage = "Must be in future or current value.";

        if (!_db.MustWhenModified(dialog,
            propertyExpression: x => x.ExpiresAt,
            predicate: x => x > _clock.UtcNowOffset))
        {
            _domainContext.AddError(nameof(UpdateDialogCommand.Dto.ExpiresAt), errorMessage);
        }

        if (!_db.MustWhenModified(dialog,
            propertyExpression: x => x.DueAt,
            predicate: x => x > _clock.UtcNowOffset || x == null))
        {
            _domainContext.AddError(nameof(UpdateDialogCommand.Dto.DueAt), errorMessage + " (Or null)");
        }
    }

    private void AppendActivity(DialogEntity dialog, UpdateDialogDto dto)
    {
        var newDialogActivities = _mapper.Map<List<DialogActivity>>(dto.Activities);

        var existingIds = _db.DialogActivities
            .Local
            .Select(x => x.Id)
            .Intersect(newDialogActivities.Select(x => x.Id))
            .ToArray();

        if (existingIds.Length != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogActivity>(existingIds));
            return;
        }

        dialog.Activities.AddRange(newDialogActivities);

        // Tell ef explicitly to add activities as new to the database.
        _db.DialogActivities.AddRange(newDialogActivities);
    }

    private void VerifyActivityTransmissionRelations(DialogEntity dialog)
    {
        var relatedTransmissionIds = dialog.Activities
            .Where(x => x.TransmissionId is not null)
            .Select(x => x.TransmissionId)
            .ToList();

        if (relatedTransmissionIds.Count == 0)
        {
            return;
        }

        var transmissionIds = dialog.Transmissions.Select(x => x.Id).ToList();

        var invalidTransmissionIds = relatedTransmissionIds
            .Where(id => !transmissionIds.Contains(id!.Value))
            .ToList();

        if (invalidTransmissionIds.Count != 0)
        {
            _domainContext.AddError(
                nameof(UpdateDialogDto.Activities),
                $"Invalid '{nameof(DialogActivity.TransmissionId)}, entity '{nameof(DialogTransmission)}'" +
                $" with the following key(s) does not exist: ({string.Join(", ", invalidTransmissionIds)}) in '{nameof(dialog.Transmissions)}'");
        }
    }

    private async Task AppendTransmission(DialogEntity dialog, UpdateDialogDto dto, CancellationToken cancellationToken)
    {
        var newDialogTransmissions = _mapper.Map<List<DialogTransmission>>(dto.Transmissions);

        // Ensure transmissions and attachments have a UUIDv7 ID, needed for the transmission hierarchy validation
        // and to guarantee deterministic order of input to output dtos.
        newDialogTransmissions.Cast<IIdentifiableEntity>()
            .Concat(newDialogTransmissions.SelectMany(x => x.Attachments))
            .EnsureIds();

        var existingIds = _db.DialogTransmissions
            .Local
            .Select(x => x.Id)
            .Intersect(newDialogTransmissions.Select(x => x.Id))
            .ToArray();

        if (existingIds.Length != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogTransmission>(existingIds));
            return;
        }

        var newAttachments = newDialogTransmissions
            .SelectMany(x => x.Attachments)
            .ToList();

        var newAttachmentIds = newAttachments
            .Select(x => x.Id);

        var existingAttachmentIds = _db.DialogTransmissions
            .Local
            .SelectMany(x => x.Attachments)
            .Select(x => x.Id)
            .Intersect(newAttachmentIds)
            .ToArray();

        if (existingAttachmentIds.Length != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogAttachment>(existingAttachmentIds));
            return;
        }

        await _transmissionHierarchyValidator.ValidateNewTransmissionsAsync(
            dialog.Id,
            newDialogTransmissions,
            cancellationToken);

        var appendResult = _dialogTransmissionAppender.Append(dialog, newDialogTransmissions);

        if (appendResult.ContainsEndUserTransmission)
        {
            AddSystemLabel(dialog, SystemLabel.Values.Sent);
        }
    }

    private IEnumerable<DialogGuiAction> CreateGuiActions(IEnumerable<GuiActionDto> creatables)
    {
        var guiActions = _mapper.Map<List<DialogGuiAction>>(creatables);
        _db.DialogGuiActions.AddRange(guiActions);
        return guiActions;
    }

    private IEnumerable<DialogApiAction> CreateApiActions(IEnumerable<ApiActionDto> creatables)
    {
        return creatables.Select(x =>
        {
            var apiAction = _mapper.Map<DialogApiAction>(x);
            apiAction.Endpoints = _mapper.Map<List<DialogApiActionEndpoint>>(x.Endpoints);
            _db.DialogApiActions.Add(apiAction);
            return apiAction;
        });
    }

    private void UpdateApiActions(IEnumerable<UpdateSet<DialogApiAction, ApiActionDto>> updateSets)
    {
        foreach (var (source, destination) in updateSets)
        {
            _mapper.Map(source, destination);

            destination.Endpoints
                .Merge(source.Endpoints,
                    destinationKeySelector: x => x.Id,
                    sourceKeySelector: x => x.Id,
                    create: _mapper.Map<List<DialogApiActionEndpoint>>,
                    update: _mapper.Update,
                    delete: DeleteDelegate.Default);
        }
    }

    private IEnumerable<DialogAttachment> CreateAttachments(IEnumerable<AttachmentDto> creatables)
    {
        return creatables.Select(attachmentDto =>
        {
            var attachment = _mapper.Map<DialogAttachment>(attachmentDto);
            // Ensure attachments have a UUIDv7 ID, needed to guarantee deterministic order of input to output dtos.
            attachment.EnsureId();
            attachment.Urls = _mapper.Map<List<AttachmentUrl>>(attachmentDto.Urls);
            _db.DialogAttachments.Add(attachment);
            ValidateTimeFields(attachment);
            return attachment;
        });
    }

    private void UpdateAttachments(IEnumerable<UpdateSet<DialogAttachment, AttachmentDto>> updateSets)
    {
        foreach (var updateSet in updateSets)
        {
            _mapper.Map(updateSet.Source, updateSet.Destination);
            ValidateTimeFields(updateSet.Destination);
            updateSet.Destination.Urls
                .Merge(updateSet.Source.Urls,
                    destinationKeySelector: x => x.Id,
                    sourceKeySelector: x => x.Id,
                    create: _mapper.Map<List<AttachmentUrl>>,
                    update: _mapper.Update,
                    delete: DeleteDelegate.Default);
        }
    }
}
