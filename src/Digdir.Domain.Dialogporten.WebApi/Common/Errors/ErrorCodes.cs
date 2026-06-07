namespace Digdir.Domain.Dialogporten.WebApi.Common.Errors;

internal static class ErrorCodes
{
    private const string UrnPrefix = "urn:altinn:error:";

    public const string Validation = "STD-00000";
    public const string ValidationError = "DP.VLD-00000";
    public const string MultipleProblems = "STD-00001";
    public const string Forbidden = "DP.AUT-00000";
    public const string NotFound = "DP.NF-00000";
    public const string NotAcceptable = "DP.NA-00000";
    public const string Conflict = "DP.CFL-00000";
    public const string Gone = "DP.GONE-00000";
    public const string PreconditionFailed = "DP.PRE-00000";
    public const string PayloadTooLarge = "DP.PYL-00000";
    public const string UnprocessableEntity = "DP.UNP-00000";
    public const string InternalServerError = "DP.SRV-00000";
    public const string BadGateway = "DP.UPS-00000";

    public static string ToUrn(string code) => UrnPrefix + code;
}
