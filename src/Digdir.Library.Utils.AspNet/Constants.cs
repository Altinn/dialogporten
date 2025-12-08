namespace Digdir.Library.Utils.AspNet;

public static class Constants
{
    public const string Ok = "OK";
    public const string Error = "ERROR";

    public const string DbStatement = "db.statement";
    public const string OtelStatusCode = "otel.status_code";
    public const string OtelStatusDescription = "otel.status_description";

    public const string FusionCache = "ZiggyCreatures";
    public const string AspNetCore = "Microsoft.AspNetCore";
    public const string Npgsql = "Npgsql";
    public const string HotChocolateDiagnostics = "HotChocolate.Diagnostics";

    // GraphQL Activity OperationNames to filter out (internal processing steps)
    public const string GraphQLOperationParseHttpRequest = "ParseHttpRequest";
    public const string GraphQLOperationValidateDocument = "ValidateDocument";
    public const string GraphQLOperationCompileOperation = "CompileOperation";
    public const string GraphQLOperationFormatHttpResponse = "FormatHttpResponse";
}
