namespace Altinn.ApiClients.Dialogporten;

/// <summary>
/// The JOSE "typ" header values of the tokens Dialogporten issues.
/// </summary>
public static class DialogTokenTypes
{
    /// <summary>
    /// The dialog token, issued per dialog and asserting the actions authorized for the dialog party.
    /// </summary>
    /// <remarks>
    /// Dialogporten deliberately keeps the generic "JWT" type on the dialog token; explicit typing per
    /// RFC 8725 is applied to the token types introduced after it.
    /// </remarks>
    public const string DialogToken = "JWT";

    /// <summary>
    /// The dialog context token, issued per authorization-context-carrying entity and asserting a single
    /// PDP-verified grant (action, effective resource) along with the parties it was permitted for.
    /// </summary>
    public const string DialogContextToken = "dialogcontexttoken+jwt";
}
