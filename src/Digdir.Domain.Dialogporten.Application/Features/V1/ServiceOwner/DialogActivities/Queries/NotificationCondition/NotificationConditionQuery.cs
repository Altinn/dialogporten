using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogActivities.Queries.NotificationCondition;

public sealed class NotificationConditionQuery : IRequest<NotificationConditionResult>
{
    public Guid DialogId { get; set; }
    public NotificationConditionType ConditionType { get; set; }
    public ActivityType.Values ActivityType { get; set; }
    public Guid? TransmissionId { get; set; }
}

public enum NotificationConditionType
{
    NotExists = 1,
    Exists = 2
}

[GenerateOneOf]
public sealed partial class NotificationConditionResult : OneOfBase<NotificationConditionDto, ValidationError, EntityNotFound>;

internal sealed class NotificationConditionQueryHandler : IRequestHandler<NotificationConditionQuery, NotificationConditionResult>
{
    private readonly IDialogDbContext _db;

    public NotificationConditionQueryHandler(IDialogDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<NotificationConditionResult> Handle(NotificationConditionQuery request, CancellationToken cancellationToken)
    {
        var dialog = await _db.Dialogs
            .AsNoTracking()
            .Include(x => x.Activities
                .Where(x => request.TransmissionId == null || x.TransmissionId == request.TransmissionId)
                .Where(x => x.TypeId == request.ActivityType))
            .FirstOrDefaultAsync(x => x.Id == request.DialogId,
                cancellationToken: cancellationToken);

        if (dialog is null)
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var conditionMet = dialog.Activities.Count == 0
            ? request.ConditionType == NotificationConditionType.NotExists
            : request.ConditionType == NotificationConditionType.Exists;

        return new NotificationConditionDto { SendNotification = conditionMet };
    }
}
