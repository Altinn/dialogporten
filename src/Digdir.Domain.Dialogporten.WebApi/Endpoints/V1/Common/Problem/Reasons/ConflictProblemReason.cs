namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Reasons;

public sealed record ConflictProblemReason : IProblemReason
{
    public required string Key { get; set; }
    public required object Value { get; set; }
    public required string Explanation { get; set; }

}
