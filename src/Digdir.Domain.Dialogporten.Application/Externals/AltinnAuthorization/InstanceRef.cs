using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public enum InstanceRefType
{
    AppInstanceId = 1,
    CorrespondenceId = 2,
    DialogId = 3
}

public readonly record struct InstanceRef(InstanceRefType Type, Guid Id, string Value, int? PartyId = null)
{
    public const string AppInstancePrefix = "urn:altinn:instance-id:";
    public const string CorrespondencePrefix = "urn:altinn:correspondence-id:";
    public const string DialogPrefix = "urn:altinn:dialog-id:";

    public static string CreateDialogRef(Guid dialogId) => DialogPrefix + dialogId;

    public static bool TryParse(string? value, [NotNullWhen(true)] out InstanceRef? instanceRef)
    {
        instanceRef = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();

        return TryParseAppInstanceRef(normalized, out instanceRef)
               || TryParseWithPrefix(normalized, CorrespondencePrefix, InstanceRefType.CorrespondenceId, out instanceRef)
               || TryParseWithPrefix(normalized, DialogPrefix, InstanceRefType.DialogId, out instanceRef);
    }

    public string ToLookupLabel() =>
        Type is not InstanceRefType.AppInstanceId || PartyId is null
            ? Value
            : $"{Constants.ServiceContextInstanceIdPrefix}{PartyId.Value}/{Id}".ToLowerInvariant();

    private static bool TryParseAppInstanceRef(string normalized, [NotNullWhen(true)] out InstanceRef? instanceRef)
    {
        instanceRef = null;

        if (!normalized.StartsWith(AppInstancePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = normalized[AppInstancePrefix.Length..];
        var separator = suffix.IndexOf('/');
        if (separator <= 0 || separator == suffix.Length - 1)
        {
            return false;
        }

        var partyPart = suffix[..separator];
        var instancePart = suffix[(separator + 1)..];

        if (!int.TryParse(partyPart, out var partyId)
            || !Guid.TryParse(instancePart, out var instanceId))
        {
            return false;
        }

        instanceRef = new InstanceRef(InstanceRefType.AppInstanceId, instanceId, normalized, partyId);
        return true;
    }

    private static bool TryParseWithPrefix(
        string normalized,
        string prefix,
        InstanceRefType type,
        [NotNullWhen(true)] out InstanceRef? instanceRef)
    {
        instanceRef = null;

        if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var idPart = normalized[prefix.Length..];
        if (!Guid.TryParse(idPart, out var guid))
        {
            return false;
        }

        instanceRef = new InstanceRef(type, guid, normalized);
        return true;
    }
}
