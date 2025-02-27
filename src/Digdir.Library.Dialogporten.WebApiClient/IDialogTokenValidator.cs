using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Altinn.ApiClients.Dialogporten;

/// <summary>
/// Represents a service that can validate a dialog token.
/// </summary>
public interface IDialogTokenValidator
{
    /// <summary>
    /// Validates a dialog token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <param name="dialogId">The optional dialog ID associated with the token. If the token does not represent this ID, the validation will fail.</param>
    /// <param name="requiredActions">The optional list of required actions for the token.</param>
    /// <param name="options">The optional validation parameters.</param>
    /// <returns>The result of the validation.</returns>
    IValidationResult Validate(ReadOnlySpan<char> token,
        Guid? dialogId = null,
        string[]? requiredActions = null,
        DialogTokenValidationParameters? options = null);
}

/// <summary>
/// Represents the parameters used to validate a dialog token.
/// </summary>
/// <remarks>
/// Global defaults are provided by and can be altered through <see cref="DialogTokenValidationParameters.Default"/>.
/// </remarks>
public class DialogTokenValidationParameters
{
    /// <summary>
    /// Gets the default validation parameters which are used when no parameters are provided.
    /// </summary>
    /// <remarks>
    /// This may be altered to change the global default behavior of the validation.
    /// </remarks>
    public static DialogTokenValidationParameters Default { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to validate the token's lifetime.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Gets or sets the clock skew to apply when validating the token's lifetime.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Represents the result of a dialog token validation. 
/// </summary>
public interface IValidationResult
{
    /// <summary>
    /// Indicates whether the token is valid based on the validation parameters.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ClaimsPrincipal))]
    bool IsValid { get; }

    /// <summary>
    /// A dictionary of errors that occurred during validation.
    /// </summary>
    Dictionary<string, List<string>> Errors { get; }

    /// <summary>
    /// The <see cref="ClaimsPrincipal"/> extracted from the token.
    /// </summary>
    /// <remarks>
    /// This property will not be null if the token is valid. It will be null if the token has an invalid format.
    /// </remarks>
    ClaimsPrincipal? ClaimsPrincipal { get; }
}
