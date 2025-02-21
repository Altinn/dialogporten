using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Altinn.ApiClients.Dialogporten.Services;

namespace Altinn.ApiClients.Dialogporten;

public interface IDialogTokenValidator
{
    IValidationResult Validate(ReadOnlySpan<char> token);
}

public interface IValidationResult
{
    [MemberNotNullWhen(true, nameof(Claims))] bool IsValid { get; }
    ClaimsPrincipal? Claims { get; set; }
    Dictionary<string, List<string>> Errors { get; }
}
