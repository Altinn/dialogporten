using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Reasons;
using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;

[OpenApiTypeName("ConflictProblemDetails")]
public sealed class ConflictProblemDetails : ProblemDetails
{
    public List<Conflict> Conflicts { get; set; } = [];
}

[OpenApiTypeName("Conflict")]
public sealed class Conflict
{
    public required string Key { get; set; }
    [OneOfTypes(typeof(string), typeof(int))]
    public required object Value { get; set; }
    public required string Reason { get; set; }
}

public static class ValidationFailuresExtensions
{
    extension(List<ValidationFailure> failures)
    {
        public List<Conflict> ToConflicts()
        {
            return failures.Select(x =>
            {
                var reason = (ConflictProblemReason)x.CustomState;
                return new Conflict
                {
                    Key = reason.Key,
                    Value = reason.Value,
                    Reason = reason.Explanation
                };
            }).ToList();
        }
    }
}
