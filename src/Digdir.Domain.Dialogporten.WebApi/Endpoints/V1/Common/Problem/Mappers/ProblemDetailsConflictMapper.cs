using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflicts;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;
using FluentValidation.Results;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Mappers;

public sealed record ProblemDetailsConflictCode
{
    private static readonly List<ProblemDetailsConflictCode> AllCodesInternal = new();
    public static IReadOnlyList<ProblemDetailsConflictCode> AllCodes => AllCodesInternal;

    public static readonly ProblemDetailsConflictCode ConcurrentOperationRejected = new("DP.CFL-00000");
    public static readonly ProblemDetailsConflictCode IdempotentKeysExist = new("DP.CFL-00001");
    public static readonly ProblemDetailsConflictCode DialogIdForIdempotentKeyExists = new("DP.CFL-00002");

    private string Code { get; }

    private ProblemDetailsConflictCode(string code)
    {
        Code = code;
        AllCodesInternal.Add(this);
    }

    public override string ToString() => Code;
}

public sealed class ProblemDetailsConflictMapper
{
    /// <summary>
    /// Implicit contract with <see cref="Conflict.ToValidationResults"/>
    /// </summary>
    public static List<ProblemDetailsConflict> ToConflicts(List<ValidationFailure> failures)
    {
        return failures
            .SelectMany(x => ToConflicts((IConflictReason)x.CustomState))
            .ToList();
    }

    public static ProblemDetailsConflict[] ToConflicts(IConflictReason reason)
    {
        return reason switch
        {
            ConcurrentOperationRejected =>
            [
                new ProblemDetailsConflict
                {
                    Code = ProblemDetailsConflictCode.ConcurrentOperationRejected.ToString(),
                    Title = "The request conflicted with a concurrent operation. Please try again."
                }
            ],
            IdempotentKeysExist x => x.IdempotentKeys.Select(key => new ProblemDetailsConflict
            {
                Code = ProblemDetailsConflictCode.IdempotentKeysExist.ToString(),
                Title = "Idempotent key already exist.",
                Extensions = new Dictionary<string, object?>
                {
                    ["idempotentKey"] = key
                }
            }).ToArray(),
            DialogIdForIdempotentKeyExists x =>
            [
                new ProblemDetailsConflict
                {
                    Code = ProblemDetailsConflictCode.DialogIdForIdempotentKeyExists.ToString(),
                    Title = "Idempotent Key caused a dialog id conflict.",
                    Extensions = new Dictionary<string, object?>
                    {
                        ["dialogId"] = x.DialogId,
                        ["idempotentKey"] = x.IdempotentKey
                    }
                }
            ],
            _ => throw new NotSupportedException(
                $"No {nameof(ProblemDetailsConflict)} mapping registered for {reason.GetType()}")
        };
    }
}
