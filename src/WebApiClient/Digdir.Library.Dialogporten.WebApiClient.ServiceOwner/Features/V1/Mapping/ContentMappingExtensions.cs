using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

/// <summary>
/// Maps the dialog-level content hierarchies. <see cref="ContentValue"/> is shared across all three
/// model families and is therefore reused by reference rather than copied.
/// </summary>
internal static class ContentMappingExtensions
{
    internal static CreateDialogContent ToCreateDialogContent(this Content source) => new()
    {
        Title = source.Title,
        NonSensitiveTitle = source.NonSensitiveTitle,
        Summary = source.Summary,
        NonSensitiveSummary = source.NonSensitiveSummary,
        SenderName = source.SenderName,
        AdditionalInfo = source.AdditionalInfo,
        ExtendedStatus = source.ExtendedStatus,
        MainContentReference = source.MainContentReference,
    };

    internal static UpdateDialogContent ToUpdateDialogContent(this Content source) => new()
    {
        Title = source.Title,
        NonSensitiveTitle = source.NonSensitiveTitle,
        Summary = source.Summary,
        NonSensitiveSummary = source.NonSensitiveSummary,
        SenderName = source.SenderName,
        AdditionalInfo = source.AdditionalInfo,
        ExtendedStatus = source.ExtendedStatus,
        MainContentReference = source.MainContentReference,
    };

    internal static UpdateDialogContent ToUpdateDialogContent(this CreateDialogContent source) => new()
    {
        Title = source.Title,
        NonSensitiveTitle = source.NonSensitiveTitle,
        Summary = source.Summary,
        NonSensitiveSummary = source.NonSensitiveSummary,
        SenderName = source.SenderName,
        AdditionalInfo = source.AdditionalInfo,
        ExtendedStatus = source.ExtendedStatus,
        MainContentReference = source.MainContentReference,
    };

    internal static CreateDialogContent ToCreateDialogContent(this UpdateDialogContent source) => new()
    {
        Title = source.Title,
        NonSensitiveTitle = source.NonSensitiveTitle,
        Summary = source.Summary,
        NonSensitiveSummary = source.NonSensitiveSummary,
        SenderName = source.SenderName,
        AdditionalInfo = source.AdditionalInfo,
        ExtendedStatus = source.ExtendedStatus,
        MainContentReference = source.MainContentReference,
    };

    internal static Content ToDialogContent(this CreateDialogContent source) => new()
    {
        Title = source.Title,
        NonSensitiveTitle = source.NonSensitiveTitle,
        Summary = source.Summary,
        NonSensitiveSummary = source.NonSensitiveSummary,
        SenderName = source.SenderName,
        AdditionalInfo = source.AdditionalInfo,
        ExtendedStatus = source.ExtendedStatus,
        MainContentReference = source.MainContentReference,
    };

    internal static Content ToDialogContent(this UpdateDialogContent source) => new()
    {
        Title = source.Title,
        NonSensitiveTitle = source.NonSensitiveTitle,
        Summary = source.Summary,
        NonSensitiveSummary = source.NonSensitiveSummary,
        SenderName = source.SenderName,
        AdditionalInfo = source.AdditionalInfo,
        ExtendedStatus = source.ExtendedStatus,
        MainContentReference = source.MainContentReference,
    };
}
