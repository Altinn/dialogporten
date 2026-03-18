using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

public enum InstanceRefType
{
    AppInstanceId = 1,
    CorrespondenceId = 2,
    DialogId = 3
}

public readonly record struct InstanceRef(InstanceRefType Type, Guid Id, string Value, int? PartyId = null)
{
    public static string CreateDialogRef(Guid dialogId) => AltinnAuthorizationConstants.DialogRefPrefix + dialogId;

    public static InstanceRef FromDialog(DialogEntity dialogEntity)
    {
        ArgumentNullException.ThrowIfNull(dialogEntity);

        var labels = dialogEntity.ServiceOwnerContext.ServiceOwnerLabels.Select(x => x.Value);
        return FromDialog(dialogEntity.Id, labels);
    }

    public static InstanceRef FromDialog(Guid dialogId, IEnumerable<string> serviceOwnerLabels)
    {
        ArgumentNullException.ThrowIfNull(serviceOwnerLabels);

        var labels = serviceOwnerLabels
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .ToList();

        var appInstanceRef = labels
            .Select(TryToAppInstanceRef)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .Select(ParseCandidate)
            .FirstOrDefault();

        if (appInstanceRef is { } appInstance)
        {
            return appInstance;
        }

        var correspondenceRef = labels
            .Where(x => x.StartsWith(AltinnAuthorizationConstants.CorrespondenceRefPrefix, StringComparison.Ordinal))
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .Select(ParseCandidate)
            .FirstOrDefault();

        return correspondenceRef ?? new InstanceRef(InstanceRefType.DialogId, dialogId, CreateDialogRef(dialogId).ToLowerInvariant());
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out InstanceRef? instanceRef)
    {
        instanceRef = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();

        return TryParseAppInstanceRef(normalized, out instanceRef)
               || TryParseWithPrefix(normalized, AltinnAuthorizationConstants.CorrespondenceRefPrefix, InstanceRefType.CorrespondenceId, out instanceRef)
               || TryParseWithPrefix(normalized, AltinnAuthorizationConstants.DialogRefPrefix, InstanceRefType.DialogId, out instanceRef);
    }

    public string ToLookupLabel() =>
        Type is not InstanceRefType.AppInstanceId || PartyId is null
            ? Value
            : $"{Constants.ServiceContextInstanceIdPrefix}{PartyId.Value}/{Id}".ToLowerInvariant();

    private static InstanceRef? ParseCandidate(string? value) =>
        string.IsNullOrWhiteSpace(value) || !TryParse(value, out var instanceRef)
            ? null
            : instanceRef;

    private static string? TryToAppInstanceRef(string labelValue)
    {
        if (labelValue.StartsWith(AltinnAuthorizationConstants.AppInstanceRefPrefix, StringComparison.Ordinal))
        {
            return labelValue.ToLowerInvariant();
        }

        if (!labelValue.StartsWith(Constants.ServiceContextInstanceIdPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var separator = labelValue.LastIndexOf('/');
        if (separator < 0 || separator == labelValue.Length - 1)
        {
            return null;
        }

        var appInstanceSuffix = labelValue[Constants.ServiceContextInstanceIdPrefix.Length..];
        return $"{AltinnAuthorizationConstants.AppInstanceRefPrefix}{appInstanceSuffix}".ToLowerInvariant();
    }

    private static bool TryParseAppInstanceRef(string normalized, [NotNullWhen(true)] out InstanceRef? instanceRef)
    {
        instanceRef = null;

        if (!normalized.StartsWith(AltinnAuthorizationConstants.AppInstanceRefPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = normalized[AltinnAuthorizationConstants.AppInstanceRefPrefix.Length..];
        var separator = suffix.IndexOf('/');
        if (separator <= 0 || separator == suffix.Length - 1)
        {
            return false;
        }

        var partyPart = suffix[..separator];
        var instancePart = suffix[(separator + 1)..];

        if (!int.TryParse(partyPart, out var partyId)
            || partyId <= 0
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
