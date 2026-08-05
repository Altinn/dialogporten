using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digdir.Domain.Dialogporten.Infrastructure.Common.Serialization;

internal static class SerializerOptions
{
    public static readonly JsonSerializerOptions CloudEventSerializerOptions = new()
    {
        PropertyNamingPolicy = new LowerCaseNamingPolicy(),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };
}

internal sealed class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) =>
        name.ToLowerInvariant();
}
