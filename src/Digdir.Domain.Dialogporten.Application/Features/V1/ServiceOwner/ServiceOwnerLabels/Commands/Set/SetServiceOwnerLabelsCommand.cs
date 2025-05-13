using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerLabels.Commands.Set;

public sealed class SetServiceOwnerLabelsCommand : IRequest<SetServiceOwnerLabelsResult>
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchServiceOwnerContextRevision { get; set; }
    public List<ServiceOwnerLabelDto> ServiceOwnerLabels { get; set; } = [];
}

public sealed record SetServiceOwnerLabelsSuccess(Guid Revision);

[GenerateOneOf]
public sealed partial class SetServiceOwnerLabelsResult : OneOfBase<SetServiceOwnerLabelsSuccess, EntityNotFound, DomainError, ValidationError, ConcurrencyError>;

internal sealed class SetServiceOwnerLabelsCommandHandler : IRequestHandler<SetServiceOwnerLabelsCommand, SetServiceOwnerLabelsResult>
{
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;



    public SetServiceOwnerLabelsCommandHandler(IDialogDbContext db, IUnitOfWork unitOfWork, IMapper mapper, IUserResourceRegistry userResourceRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public async Task<SetServiceOwnerLabelsResult> Handle(
        SetServiceOwnerLabelsCommand request,
        CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var dialog = await _db
            .Dialogs
            .Include(x => x.ServiceOwnerContext)
                .ThenInclude(x => x.Labels)
            .Where(x => resourceIds.Contains(x.ServiceResource))
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        dialog.ServiceOwnerContext.Labels
            .Merge(request.ServiceOwnerLabels,
                destinationKeySelector: x => x.Value,
                sourceKeySelector: x => x.Value,
                create: _mapper.Map<List<ServiceOwnerLabel>>,
                delete: DeleteDelegate.NoOp,
                comparer: StringComparer.InvariantCultureIgnoreCase);

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(dialog.ServiceOwnerContext,
                request.IfMatchServiceOwnerContextRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<SetServiceOwnerLabelsResult>(
            success => new SetServiceOwnerLabelsSuccess(dialog.ServiceOwnerContext.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }
}
