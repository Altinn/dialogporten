using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;

public sealed class CreateTransmissionCommand : IRequest<CreateTransmissionResult>, ISilentUpdater, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchDialogRevision { get; set; }
    public List<CreateTransmissionDto> Transmissions { get; set; } = [];
    public bool IsSilentUpdate { get; set; }

    Guid IFeatureMetricServiceResourceThroughDialogIdRequest.DialogId => DialogId;
}

[GenerateOneOf]
public sealed partial class CreateTransmissionResult : OneOfBase<CreateTransmissionSuccess, EntityNotFound, EntityDeleted, ValidationError, Forbidden, DomainError, ConcurrencyError, Conflict>;

public sealed record CreateTransmissionSuccess(Guid Revision, IReadOnlyCollection<Guid> TransmissionIds);

internal sealed class CreateTransmissionCommandHandler : IRequestHandler<CreateTransmissionCommand, CreateTransmissionResult>
{
    private readonly IDialogDbContext _db;
    private readonly IDomainContext _domainContext;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceResourceAuthorizer _serviceResourceAuthorizer;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IUser _user;
    private readonly IDialogTransmissionAppender _dialogTransmissionAppender;
    private readonly ITransmissionHierarchyValidator _transmissionHierarchyValidator;

    public CreateTransmissionCommandHandler(
        IDialogDbContext db,
        IDomainContext domainContext,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IServiceResourceAuthorizer serviceResourceAuthorizer,
        IUserResourceRegistry userResourceRegistry,
        IUser user,
        IDialogTransmissionAppender dialogTransmissionAppender,
        ITransmissionHierarchyValidator transmissionHierarchyValidator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _serviceResourceAuthorizer = serviceResourceAuthorizer ?? throw new ArgumentNullException(nameof(serviceResourceAuthorizer));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _dialogTransmissionAppender = dialogTransmissionAppender ?? throw new ArgumentNullException(nameof(dialogTransmissionAppender));
        _transmissionHierarchyValidator = transmissionHierarchyValidator ?? throw new ArgumentNullException(nameof(transmissionHierarchyValidator));
    }

    public async Task<CreateTransmissionResult> Handle(CreateTransmissionCommand request, CancellationToken cancellationToken)
    {
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

        foreach (var transmission in request.Transmissions)
        {
            transmission.Id = transmission.Id.CreateVersion7IfDefault();
        }

        // Map incoming DTOs to domain entities without loading existing transmissions.
        var newTransmissions = _mapper.Map<List<DialogTransmission>>(request.Transmissions);
        var idempotentKeyHashSet = new HashSet<string>();
        foreach (var transmission in newTransmissions)
        {
            transmission.DialogId = dialog.Id;
            transmission.Dialog = dialog;

            if (transmission.IdempotentKey != null && !idempotentKeyHashSet.Add(transmission.IdempotentKey))
            {
                return new Conflict(nameof(transmission.IdempotentKey),
                    $"Duplicate of transmission idempotentKey '{transmission.IdempotentKey}'");
            }
        }

        var conflictingTransmission =
            await GetFirstTransmissionWithReusedIdempotentKey(newTransmissions, cancellationToken);
        if (conflictingTransmission is not null)
        {
            return new Conflict(nameof(conflictingTransmission.IdempotentKey),
                $"'{conflictingTransmission.IdempotentKey}' already exists with DialogId '{dialog.Id}'");
        }

        await _transmissionHierarchyValidator.ValidateNewTransmissionsAsync(
            dialog.Id,
            newTransmissions,
            cancellationToken);

        var appendResult = _dialogTransmissionAppender.Append(dialog, newTransmissions);

        if (appendResult.ContainsEndUserTransmission)
        {
            AddSystemLabel(dialog, SystemLabel.Values.Sent);
        }

        // Any service-owner transmission introduces unopened content, so mark the dialog accordingly.
        if (appendResult.ContainsServiceOwnerTransmission)
        {
            dialog.HasUnopenedContent = true;
        }

        var authorizeResult = await _serviceResourceAuthorizer.AuthorizeServiceResources(dialog, cancellationToken);
        if (authorizeResult.Value is Forbidden forbidden)
        {
            _domainContext.Pop();
            return forbidden;
        }

        if (!request.IsSilentUpdate)
        {
            AddSystemLabel(dialog, SystemLabel.Values.Default);
        }

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog, request.IfMatchDialogRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<CreateTransmissionResult>(
            success => new CreateTransmissionSuccess(dialog.Revision, newTransmissions.Select(x => x.Id).ToArray()),
            domainError => domainError,
            concurrencyError => concurrencyError);
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

    private async Task<DialogTransmission?> GetFirstTransmissionWithReusedIdempotentKey(List<DialogTransmission> transmissions,
        CancellationToken cancellationToken)
    {
        var idempotentKeys = transmissions.Select(i => i.IdempotentKey).OfType<string>().ToList();
        if (idempotentKeys.IsNullOrEmpty()) return null;

        var transmission = await _db.DialogTransmissions
            .Where(x => x.IdempotentKey != null && x.DialogId == transmissions.First().DialogId && idempotentKeys.Contains(x.IdempotentKey))
            .FirstOrDefaultAsync(cancellationToken);

        return transmission;
    }
}
