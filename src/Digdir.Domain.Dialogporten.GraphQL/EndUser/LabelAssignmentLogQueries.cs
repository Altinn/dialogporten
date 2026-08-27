using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.LabelAssignmentLog;
using MediatR;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public sealed partial class Queries
{
    public async Task<LabelAssignmentLogPayload> GetLabelAssignmentLog(
        [Service] ISender mediator,
        [Argument] Guid dialogId,
        CancellationToken cancellationToken)
    {
        var request = new SearchLabelAssignmentLogQuery { DialogId = dialogId };

        var result = await mediator.Send(request, cancellationToken);

        return result.Match(
            logs => new LabelAssignmentLogPayload
            {
                LabelAssignmentLog = logs.ToLabelAssignmentLogs()
            },
            notFound => new LabelAssignmentLogPayload
            {
                Errors = [new LabelAssignmentLogNotFound { Message = notFound.Message }]
            },
            deleted => new LabelAssignmentLogPayload
            {
                Errors = [new LabelAssignmentLogDeleted { Message = deleted.Message }]
            },
            forbidden => new LabelAssignmentLogPayload
            {
                Errors = forbidden.Reasons.Count > 0
                    ? [.. forbidden.Reasons.Select(x => new LabelAssignmentLogForbidden { Message = x })]
                    : [new LabelAssignmentLogForbidden()]
            });
    }
}
