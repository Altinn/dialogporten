using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Altinn.ApiClients.Dialogporten.Services;

internal sealed class DefaultValidationResult : IValidationResult
{
    public Dictionary<string, List<string>> Errors { get; } = [];
    [MemberNotNullWhen(true, nameof(Claims))] public bool IsValid => Errors.Count == 0;
    public ClaimsPrincipal? Claims { get; set; }

    internal void AddError(string key, string message)
    {
        if (!Errors.TryGetValue(key, out var messages))
        {
            Errors[key] = messages = [];
        }

        messages.Add(message);
    }
}
