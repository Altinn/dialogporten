using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.NotificationCondition;

public sealed class NotificationConditionQuery : IRequest<NotificationConditionResult>, IFeatureMetricServiceResourceThroughDialogIdRequest
{
    public Guid DialogId { get; set; }
    public NotificationConditionType ConditionType { get; set; }
    public DialogActivityType.Values ActivityType { get; set; }
    public Guid? TransmissionId { get; set; }
}

public enum NotificationConditionType
{
    NotExists = 1,
    Exists = 2
}

[GenerateOneOf]
public sealed partial class NotificationConditionResult : OneOfBase<NotificationConditionDto, ValidationError, EntityNotFound, EntityDeleted>;

internal sealed class NotificationConditionQueryHandler : IRequestHandler<NotificationConditionQuery, NotificationConditionResult>
{
    private readonly IDialogDbContext _db;

    public NotificationConditionQueryHandler(IDialogDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<NotificationConditionResult> Handle(NotificationConditionQuery request, CancellationToken cancellationToken)
    {
        var hasMatchingActivity = await _db.WrapWithRepeatableRead((dbCtx, ct) =>
            dbCtx.Dialogs
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(d => d.Id == request.DialogId)
                .Select(d => (bool?)d.Activities
                    .Where(a => a.TypeId == request.ActivityType)
                    .Any(a => request.TransmissionId == null || a.TransmissionId == request.TransmissionId))
                .FirstOrDefaultAsync(ct),
            cancellationToken);

        if (hasMatchingActivity is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var conditionMet = !hasMatchingActivity.Value
            ? request.ConditionType == NotificationConditionType.NotExists
            : request.ConditionType == NotificationConditionType.Exists;

        return new NotificationConditionDto { SendNotification = conditionMet };
    }
}
