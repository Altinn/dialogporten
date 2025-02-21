using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Altinn.ApiClients.Dialogporten;

public interface IDialogTokenValidator
{
    IValidationResult Validate(ReadOnlySpan<char> token);
}

public interface IValidationResult
{
    [MemberNotNullWhen(true, nameof(ClaimsPrincipal))]
    bool IsValid { get; }
    Dictionary<string, List<string>> Errors { get; }
    ClaimsPrincipal? ClaimsPrincipal { get; }
}
