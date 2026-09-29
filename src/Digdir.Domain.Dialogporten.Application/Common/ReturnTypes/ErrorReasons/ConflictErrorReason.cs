namespace Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.ErrorReasons;

public sealed record ConflictErrorReason : IErrorReason
{
    public required string Key { get; set; }
    public required object Value { get; set; }
    public required string Explanation { get; set; }
}
