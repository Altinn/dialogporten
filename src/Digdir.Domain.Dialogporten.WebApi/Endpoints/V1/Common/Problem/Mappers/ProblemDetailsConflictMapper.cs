using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflicts;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Mappers;

public sealed class ProblemDetailsConflictMapper
{
    public static readonly string ConcurrentOperationRejectedProblemCode = "DP.CFL-00000";
    public static readonly string IdempotentKeysExistProblemCode = "DP.CFL-00001";
    public static readonly string DialogIdForIdempotentKeyExistsProblemCode = "DP.CFL-00002";

    public static ProblemDetailsConflict Map(IConflictReason reason)
    {
        return reason switch
        {
            ConcurrentOperationRejected => new ProblemDetailsConflict
            {
                Code = ConcurrentOperationRejectedProblemCode,
                Title = "The request conflicted with a concurrent operation. Please try again."
            },
            IdempotentKeysExist x => new ProblemDetailsConflict
            {
                Code = IdempotentKeysExistProblemCode,
                Title = "One or more Idempotent keys already exist.",
                Extensions = new Dictionary<string, object?>
                {
                    ["idempotentKeys"] = x.IdempotentKeys
                }
            },
            DialogIdForIdempotentKeyExists x => new ProblemDetailsConflict
            {
                Code = DialogIdForIdempotentKeyExistsProblemCode,
                Title = "Idempotent Key caused a dialog id conflict.",
                Extensions = new Dictionary<string, object?>
                {
                    ["dialogId"] = x.DialogId,
                    ["idempotentKey"] = x.IdempotentKey
                }
            },
            _ => throw new NotSupportedException(
                $"No {nameof(ProblemDetailsConflict)} mapping registered for {reason.GetType()}")
        };
    }
}
