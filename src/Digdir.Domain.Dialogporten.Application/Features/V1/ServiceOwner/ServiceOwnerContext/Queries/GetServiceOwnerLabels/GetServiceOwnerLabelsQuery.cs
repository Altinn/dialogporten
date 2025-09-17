using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabels;

public sealed class GetServiceOwnerLabelsQuery : IRequest<GetServiceOwnerLabelsResult>, IFeatureMetricsServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
}

[GenerateOneOf]
public sealed partial class GetServiceOwnerLabelsResult : OneOfBase<ServiceOwnerLabelResultDto, EntityNotFound>;

internal sealed class GetServiceOwnerLabelsQueryHandler : IRequestHandler<GetServiceOwnerLabelsQuery, GetServiceOwnerLabelsResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public GetServiceOwnerLabelsQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IUserResourceRegistry userResourceRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public async Task<GetServiceOwnerLabelsResult> Handle(GetServiceOwnerLabelsQuery request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var serviceOwnerContext = await _db
            .DialogServiceOwnerContexts
            .Include(x => x.ServiceOwnerLabels)
            .Where(x => x.DialogId == request.DialogId)
            .Where(x => resourceIds.Contains(x.Dialog.ServiceResource))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (serviceOwnerContext is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        return new ServiceOwnerLabelResultDto
        {
            Revision = serviceOwnerContext.Revision,
            Labels = _mapper.Map<List<ServiceOwnerLabelDto>>(serviceOwnerContext.ServiceOwnerLabels),
        };
    }
}
