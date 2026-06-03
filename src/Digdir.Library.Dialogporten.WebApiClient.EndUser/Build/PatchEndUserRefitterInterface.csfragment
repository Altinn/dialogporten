if (!File.Exists(GeneratedFile))
{
    return true;
}

const string usingLine = "using Altinn.ApiClients.Maskinporten.Models;";
const string requestPropertyParameter = "[Property(\"Altinn.ApiClients.Maskinporten.TokenRequestContext\")] MaskinportenTokenRequestContext? requestContext = null";
var original = File.ReadAllText(GeneratedFile);
var updated = original;

if (updated.IndexOf(usingLine, StringComparison.Ordinal) < 0)
{
    updated = updated.Replace("using Refit;\n", "using Refit;\n" + usingLine + "\n");
}

updated = updated.Replace(", [Property] object? requestProperties = null", string.Empty);
updated = Regex.Replace(
    updated,
    @"^(?!.*MaskinportenTokenRequestContext\? requestContext)(?<method>\s*Task<.+\(.*)CancellationToken cancellationToken = default\);$",
    "${method}" + requestPropertyParameter + ", CancellationToken cancellationToken = default);",
    RegexOptions.Multiline);

if (!string.Equals(original, updated, StringComparison.Ordinal))
{
    File.WriteAllText(GeneratedFile, updated, Encoding.UTF8);
}
