using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.SystemLabelAdder;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

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
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IServiceResourceAuthorizer _serviceResourceAuthorizer;
    private readonly IMapper _mapper;
    private readonly ISystemLabelAdder _systemLabelAdder;

    public CreateActivityCommandHandler(
        IDomainContext domainContext,
        IUnitOfWork unitOfWork,
        IDialogDbContext db,
        IUserResourceRegistry userResourceRegistry,
        IServiceResourceAuthorizer serviceResourceAuthorizer,
        IMapper mapper,
        ISystemLabelAdder systemLabelAdder)
    {
        _domainContext = domainContext;
        _unitOfWork = unitOfWork;
        _db = db;
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

        if (dialog.Deleted)
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
}
