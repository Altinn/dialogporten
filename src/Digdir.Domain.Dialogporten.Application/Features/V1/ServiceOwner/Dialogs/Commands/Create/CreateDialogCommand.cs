using System.Diagnostics;
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
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

public sealed class CreateDialogCommand : IRequest<CreateDialogResult>, ISilentUpdater, IFeatureMetricServiceResourceRequest
{
    public bool IsSilentUpdate { get; set; }
    public CreateDialogDto Dto { get; set; } = null!;
    string IFeatureMetricServiceResourceRequest.ServiceResource => Dto.ServiceResource;
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
    private readonly ITransmissionHierarchyValidator _transmissionHierarchyValidator;
    private readonly IUser _user;

    public CreateDialogCommandHandler(
        IUser user,
        IDialogDbContext db,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IDomainContext domainContext,
        IResourceRegistry resourceRegistry,
        IServiceResourceAuthorizer serviceResourceAuthorizer,
        ITransmissionHierarchyValidator transmissionHierarchyValidator)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _serviceResourceAuthorizer = serviceResourceAuthorizer ?? throw new ArgumentNullException(nameof(serviceResourceAuthorizer));
        _transmissionHierarchyValidator = transmissionHierarchyValidator ?? throw new ArgumentNullException(nameof(transmissionHierarchyValidator));
    }

    public async Task<CreateDialogResult> Handle(CreateDialogCommand request, CancellationToken cancellationToken)
    {
        var dialog = _mapper.Map<DialogEntity>(request.Dto);

        // Ensure transmissions and attachments have a UUIDv7 ID, needed for the transmission hierarchy validation
        // and to guarantee deterministic order of input to output dtos.
        dialog.Transmissions.Cast<IIdentifiableEntity>()
            .Concat(dialog.Transmissions.SelectMany(x => x.Attachments))
            .Concat(dialog.Attachments)
            .EnsureIds();

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
        CreateDialogServiceOwnerContext(request, dialog);

        var activityTypes = dialog.Activities
            .Select(x => x.TypeId)
            .Distinct();

        if (!ActivityTypeAuthorization.UsingAllowedActivityTypes(activityTypes, _user, out var errorMessage))
        {
            return new Forbidden(errorMessage);
        }

        dialog.HasUnopenedContent = DialogUnopenedContent.HasUnopenedContent(dialog, serviceResourceInformation);
        _transmissionHierarchyValidator.ValidateWholeAggregate(dialog);

        var (fromParty, fromServiceOwner) = dialog.Transmissions.GetTransmissionCounts();
        dialog.FromPartyTransmissionsCount = checked((short)fromParty);
        dialog.FromServiceOwnerTransmissionsCount = checked((short)fromServiceOwner);

        if (dialog.Transmissions.ContainsTransmissionByEndUser())
        {
            AddSystemLabel(dialog, SystemLabel.Values.Sent);
        }

        _db.Dialogs.Add(dialog);

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
        dialog.EndUserContext = new();

        if (!request.Dto.SystemLabel.HasValue)
        {
            // Adding default label
            dialog.EndUserContext
                .DialogEndUserContextSystemLabels
                .Add(new());

            return;
        }

        AddSystemLabel(dialog, request.Dto.SystemLabel.Value);
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

    private void CreateDialogServiceOwnerContext(CreateDialogCommand request, DialogEntity dialog)
    {
        dialog.ServiceOwnerContext = new();
        if (request.Dto.ServiceOwnerContext?.ServiceOwnerLabels.Count > 0)
        {
            dialog.ServiceOwnerContext.ServiceOwnerLabels =
                _mapper.Map<List<DialogServiceOwnerLabel>>(request.Dto.ServiceOwnerContext.ServiceOwnerLabels);
        }
    }
}
