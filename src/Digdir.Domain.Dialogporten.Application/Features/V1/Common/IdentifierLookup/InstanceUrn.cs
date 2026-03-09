namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

public enum InstanceUrnType
{
    AppInstanceId = 1,
    CorrespondenceId = 2,
    DialogId = 3
}

public readonly record struct InstanceUrn(InstanceUrnType Type, Guid Id, string Value)
{
    public const string AppInstancePrefix = "urn:altinn:app-instance-id:";
    public const string CorrespondencePrefix = "urn:altinn:correspondence-id:";
    public const string DialogPrefix = "urn:altinn:dialog-id:";

    public static string CreateDialogUrn(Guid dialogId) => DialogPrefix + dialogId;

    public static bool TryParse(string? value, out InstanceUrn urn)
    {
        urn = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (TryParseWithPrefix(normalized, AppInstancePrefix, InstanceUrnType.AppInstanceId, out urn)
            || TryParseWithPrefix(normalized, CorrespondencePrefix, InstanceUrnType.CorrespondenceId, out urn)
            || TryParseWithPrefix(normalized, DialogPrefix, InstanceUrnType.DialogId, out urn))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseWithPrefix(
        string normalized,
        string prefix,
        InstanceUrnType type,
        out InstanceUrn urn)
    {
        urn = default;

        if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var idPart = normalized[prefix.Length..];
        if (!Guid.TryParse(idPart, out var guid))
        {
            return false;
        }

        urn = new InstanceUrn(type, guid, normalized);
        return true;
    }
}
