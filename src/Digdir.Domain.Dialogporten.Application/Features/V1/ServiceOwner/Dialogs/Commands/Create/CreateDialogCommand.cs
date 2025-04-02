using System.Diagnostics;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

public sealed class CreateDialogCommand : IRequest<CreateDialogResult>, ISilentUpdater
{
    public bool IsSilentUpdate { get; init; }
    public CreateDialogDto Dto { get; set; } = null!;
}

public sealed record CreateDialogSuccess(Guid DialogId, Guid Revision);

[GenerateOneOf]
public sealed partial class CreateDialogResult : OneOfBase<CreateDialogSuccess, DomainError, ValidationError, Forbidden, Conflict>;

internal sealed class CreateDialogCommandHandler : IRequestHandler<CreateDialogCommand, CreateDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainContext _domainContext;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IServiceResourceAuthorizer _serviceResourceAuthorizer;
    private readonly IUser _user;

    public CreateDialogCommandHandler(
        IUser user,
        IDialogDbContext db,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IDomainContext domainContext,
        IResourceRegistry resourceRegistry,
        IServiceResourceAuthorizer serviceResourceAuthorizer)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _serviceResourceAuthorizer = serviceResourceAuthorizer ?? throw new ArgumentNullException(nameof(serviceResourceAuthorizer));
    }

    public async Task<CreateDialogResult> Handle(CreateDialogCommand request, CancellationToken cancellationToken)
    {
        var dialog = _mapper.Map<DialogEntity>(request.Dto);

        await _serviceResourceAuthorizer.SetResourceType(dialog, cancellationToken);
        var serviceResourceAuthorizationResult = await _serviceResourceAuthorizer.AuthorizeServiceResources(dialog, cancellationToken);
        if (serviceResourceAuthorizationResult.Value is Forbidden forbiddenResult)
        {
            return forbiddenResult;
        }

        var serviceResourceInformation = await _resourceRegistry.GetResourceInformation(dialog.ServiceResource, cancellationToken);
        if (serviceResourceInformation is null)
        {
            _domainContext.AddError(new DomainFailure(nameof(DialogEntity.Org),
                "Cannot find service owner organization shortname for referenced service resource."));
        }
        else
        {
            dialog.Org = serviceResourceInformation.OwnOrgShortName;
        }

        var dialogId = await GetExistingDialogIdByIdempotentKey(dialog, cancellationToken);
        if (dialogId is not null)
        {
            return new Conflict(nameof(dialog.IdempotentKey), $"'{dialog.IdempotentKey}' already exists with DialogId '{dialogId}'");
        }

        CreateDialogEndUserContext(request, dialog);
        await EnsureNoExistingUserDefinedIds(dialog, cancellationToken);

        var activityTypes = dialog.Activities
            .Select(x => x.TypeId)
            .Distinct();

        if (!ActivityTypeAuthorization.UsingAllowedActivityTypes(activityTypes, _user, out var errorMessage))
        {
            return new Forbidden(errorMessage);
        }

        // Ensure transmissions have a UUIDv7 ID, needed for the transmission hierarchy validation.
        foreach (var transmission in dialog.Transmissions)
        {
            transmission.Id = transmission.Id.CreateVersion7IfDefault();
        }
        _domainContext.AddErrors(dialog.Transmissions.ValidateReferenceHierarchy(
            keySelector: x => x.Id,
            parentKeySelector: x => x.RelatedTransmissionId,
            propertyName: nameof(CreateDialogDto.Transmissions),
            maxDepth: 20,
            maxWidth: 20));

        await _db.Dialogs.AddAsync(dialog, cancellationToken);

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.Match<CreateDialogResult>(
            success => new CreateDialogSuccess(dialog.Id, dialog.Revision),
            domainError => domainError,
            concurrencyError => throw new UnreachableException("Should never get a concurrency error when creating a new dialog"));
    }



    private async Task<Guid?> GetExistingDialogIdByIdempotentKey(DialogEntity dialog, CancellationToken cancellationToken)
    {
        if (dialog.IdempotentKey is null || string.IsNullOrEmpty(dialog.Org))
        {
            return null;
        }
        var dialogId = await _db.Dialogs
            .Where(x => x.Org == dialog.Org && x.IdempotentKey == dialog.IdempotentKey)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return dialogId == Guid.Empty ? null : dialogId;
    }

    private void CreateDialogEndUserContext(CreateDialogCommand request, DialogEntity dialog)
    {
        dialog.DialogEndUserContext = new();
        if (!request.Dto.SystemLabel.HasValue)
        {
            return;
        }

        if (!_user.TryGetOrganizationNumber(out var organizationNumber))
        {
            _domainContext.AddError(new DomainFailure(nameof(organizationNumber), "Cannot find organization number for current user."));
            return;
        }

        dialog.DialogEndUserContext.UpdateLabel(
            request.Dto.SystemLabel.Value,
            $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}{organizationNumber}",
            ActorType.Values.ServiceOwner);
    }

    private async Task EnsureNoExistingUserDefinedIds(DialogEntity dialog, CancellationToken cancellationToken)
    {
        var existingDialogIds = await _db.GetExistingIds([dialog], cancellationToken);
        if (existingDialogIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogEntity>(existingDialogIds));
        }

        var existingActivityIds = await _db.GetExistingIds(dialog.Activities, cancellationToken);
        if (existingActivityIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogActivity>(existingActivityIds));
        }

        var existingTransmissionIds = await _db.GetExistingIds(dialog.Transmissions, cancellationToken);
        if (existingTransmissionIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogTransmission>(existingTransmissionIds));
        }

        var existingTransmissionAttachmentIds = await _db.GetExistingIds(dialog.Transmissions.SelectMany(t => t.Attachments), cancellationToken);
        if (existingTransmissionAttachmentIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogTransmissionAttachment>(existingTransmissionAttachmentIds));
        }

        var existingAttachmentIds = await _db.GetExistingIds(dialog.Attachments, cancellationToken);
        if (existingAttachmentIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogAttachment>(existingAttachmentIds));
        }

        var existingGuiActionIds = await _db.GetExistingIds(dialog.GuiActions, cancellationToken);
        if (existingGuiActionIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogGuiAction>(existingGuiActionIds));
        }

        var existingApiActionIds = await _db.GetExistingIds(dialog.ApiActions, cancellationToken);
        if (existingApiActionIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogApiAction>(existingApiActionIds));
        }
    }

}
