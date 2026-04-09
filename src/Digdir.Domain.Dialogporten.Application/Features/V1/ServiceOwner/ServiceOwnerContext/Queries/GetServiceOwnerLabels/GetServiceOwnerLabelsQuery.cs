using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabels;

public sealed class GetServiceOwnerLabelsQuery : IRequest<GetServiceOwnerLabelsResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
}

[GenerateOneOf]
public sealed partial class GetServiceOwnerLabelsResult : OneOfBase<ServiceOwnerLabelResultDto, EntityNotFound>;

internal sealed class GetServiceOwnerLabelsQueryHandler : IRequestHandler<GetServiceOwnerLabelsQuery, GetServiceOwnerLabelsResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public GetServiceOwnerLabelsQueryHandler(
        IDialogDbContext db,
        IUserResourceRegistry userResourceRegistry)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(userResourceRegistry);

        _db = db;
        _userResourceRegistry = userResourceRegistry;
    }

    public async Task<GetServiceOwnerLabelsResult> Handle(GetServiceOwnerLabelsQuery request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        var serviceOwnerContext = await _db.WrapWithRepeatableRead((dbCtx, ct) =>
            dbCtx.DialogServiceOwnerContexts
                .Include(x => x.ServiceOwnerLabels)
                .Where(x => x.DialogId == request.DialogId)
                .Where(x => resourceIds.Contains(x.Dialog.ServiceResource))
                .FirstOrDefaultAsync(cancellationToken: ct),
            cancellationToken);

        if (serviceOwnerContext is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        return new ServiceOwnerLabelResultDto
        {
            Revision = serviceOwnerContext.Revision,
            Labels = [.. serviceOwnerContext.ServiceOwnerLabels.Select(x => x.ToDto())],
        };
    }
}
