namespace Digdir.Domain.Dialogporten.Domain.Common;

public static class Constants
{
    public const int MinSearchStringLength = 3;
    public const int MaxSearchTagLength = 63;
    public const int DefaultMaxStringLength = 255;
    public const int DefaultMaxUriLength = 1023;
    public const int CorrespondenceMaxStringLength = 512;
    public const int CorrespondenceActivityDescriptionMaxLength = 4095;
    public const int MaxIdempotentKeyLength = 36;
    public const int MinIdempotentKeyLength = 3;

    public const string ServiceResourcePrefix = "urn:altinn:resource:";
    public const string AppResourcePrefix = "urn:altinn:app:";
    public const string AppResourceIdPrefix = "app_";
    public const string OrgResourcePrefix = "urn:altinn:org:";
    public const string ServiceContextInstanceIdPrefix = "urn:altinn:integration:storage:";

    public const string IsSilentUpdate = "IsSilentUpdate";

    /// <summary>
    /// Fallback system user name.
    /// This fallback is used when the party name registry has not yet synchronized the system name.
    /// The name may not be searchable for up to 1 minute + processing time.
    /// This fallback should probably be replaced if/when the party name registry imrpoves this situation.
    /// </summary>
    public const string FallbackSystemUsername = "Systembruker";
}
