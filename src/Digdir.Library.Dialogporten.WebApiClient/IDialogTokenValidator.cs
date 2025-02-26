using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Altinn.ApiClients.Dialogporten;

public interface IDialogTokenValidator
{
    IValidationResult Validate(ReadOnlySpan<char> token,
        Guid? dialogId = null,
        string[]? requiredActions = null,
        DialogTokenValidationParameters? options = null);
}

public class DialogTokenValidationParameters
{
    public static DialogTokenValidationParameters Default { get; } = new();
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuer { get; set; } = true;
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(10);
}

public interface IValidationResult
{
    [MemberNotNullWhen(true, nameof(ClaimsPrincipal))]
    bool IsValid { get; }
    Dictionary<string, List<string>> Errors { get; }
    ClaimsPrincipal? ClaimsPrincipal { get; }
}
