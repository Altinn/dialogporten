using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Digdir.Domain.Dialogporten.Application.Externals.Presentation;

public interface IUser
{
    ClaimsPrincipal GetPrincipal();
    string GetSystemUserOrg();
}

public sealed class AuthorizationDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("systemuser_id")]
    public List<string> SystemuserId { get; set; } = null!;

    [JsonPropertyName("systemuser_org")]
    public SystemuserOrg Org { get; set; } = new();

    [JsonPropertyName("system_id")]
    public string SystemId { get; set; } = null!;
}

public sealed class SystemuserOrg
{
    [JsonPropertyName("authority")]
    public string Authority { get; set; } = null!;

    [JsonPropertyName("ID")]
    public string Id { get; set; } = null!;
}
