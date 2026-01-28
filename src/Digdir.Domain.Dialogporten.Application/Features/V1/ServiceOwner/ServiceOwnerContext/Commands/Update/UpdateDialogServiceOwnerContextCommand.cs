using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;

public sealed class UpdateDialogServiceOwnerContextCommand : IRequest<UpdateDialogServiceOwnerContextResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public Guid? IfMatchServiceOwnerContextRevision { get; set; }
    public UpdateServiceOwnerContextDto Dto { get; set; } = null!;
}

[GenerateOneOf]
public sealed partial class UpdateDialogServiceOwnerContextResult :
    OneOfBase<UpdateServiceOwnerContextSuccess, ValidationError, EntityNotFound, DomainError, ConcurrencyError, Conflict>;

public sealed record UpdateServiceOwnerContextSuccess(Guid Revision);

internal sealed class UpdateDialogServiceOwnerContextCommandHandler :
    IRequestHandler<UpdateDialogServiceOwnerContextCommand, UpdateDialogServiceOwnerContextResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDataLoaderContext _dataLoaderContext;
    private readonly IMapper _mapper;

    public UpdateDialogServiceOwnerContextCommandHandler(
        IUnitOfWork unitOfWork,
        IDataLoaderContext dataLoaderContext, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dataLoaderContext = dataLoaderContext ?? throw new ArgumentNullException(nameof(dataLoaderContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<UpdateDialogServiceOwnerContextResult> Handle(UpdateDialogServiceOwnerContextCommand request,
        CancellationToken cancellationToken)
    {
        var serviceOwnerContext = UpdateServiceOwnerContextDataLoader.GetPreloadedData(_dataLoaderContext);

        if (serviceOwnerContext is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        serviceOwnerContext.ServiceOwnerLabels
            .Merge(request.Dto.ServiceOwnerLabels,
                destinationKeySelector: x => x.Value,
                sourceKeySelector: x => x.Value,
                create: _mapper.Map<List<DialogServiceOwnerLabel>>,
                delete: DeleteDelegate.Default,
                comparer: StringComparer.InvariantCultureIgnoreCase);

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(serviceOwnerContext, request.IfMatchServiceOwnerContextRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<UpdateDialogServiceOwnerContextResult>(
            success => new UpdateServiceOwnerContextSuccess(serviceOwnerContext.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError,
            conflict => conflict);
    }
}
