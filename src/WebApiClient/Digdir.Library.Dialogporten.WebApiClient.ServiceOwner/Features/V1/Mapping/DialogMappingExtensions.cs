using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

/// <summary>
/// Conversions between the three parallel dialog model families: the <see cref="Dialog"/> response DTO,
/// the <see cref="CreateDialog"/> POST body and the <see cref="UpdateDialog"/> PUT body.
/// <br/><br/>
/// These conversions are lossy by design. Fields that have no target on the destination model are dropped
/// (for example, when going to <see cref="UpdateDialog"/> the identity, party, visibility and system-label
/// fields are not carried), and read-only server fields on <see cref="Dialog"/> (revision, counts, contexts,
/// seen-log, etc.) cannot be recovered after a round-trip. Shared value types (<c>ContentValue</c>,
/// <c>Actor</c>, <c>Localization</c>) are reused by reference rather than copied.
/// </summary>
public static class DialogMappingExtensions
{
    /// <summary>
    /// Maps a <see cref="Dialog"/> to an <see cref="UpdateDialog"/> for a read-modify-write (PUT) flow.
    /// Identity, party, visibility, created/updated timestamps and the obsolete system label are not part of
    /// the update body and are dropped.
    /// </summary>
    public static UpdateDialog ToUpdateDialog(this Dialog source) => new()
    {
        Progress = source.Progress,
        ExtendedStatus = source.ExtendedStatus,
        ExternalReference = source.ExternalReference,
        DueAt = source.DueAt,
        Process = source.Process,
        PrecedingProcess = source.PrecedingProcess,
        ExpiresAt = source.ExpiresAt,
        IsApiOnly = source.IsApiOnly,
        Status = source.Status.ToDialogStatusInput(),
        Content = source.Content?.ToUpdateDialogContent(),
        SearchTags = source.SearchTags?.Select(x => x.ToUpdateDialogTag()).ToList(),
        Attachments = source.Attachments?.Select(x => x.ToUpdateDialogAttachment()).ToList(),
        Transmissions = source.Transmissions?.Select(x => x.ToUpdateDialogTransmission()).ToList(),
        GuiActions = source.GuiActions?.Select(x => x.ToUpdateDialogGuiAction()).ToList(),
        ApiActions = source.ApiActions?.Select(x => x.ToUpdateDialogApiAction()).ToList(),
        Activities = source.Activities?.Select(x => x.ToUpdateDialogActivity()).ToList(),
    };

    /// <summary>
    /// Maps a <see cref="Dialog"/> to a <see cref="CreateDialog"/>, typically to clone/duplicate an existing
    /// dialog. By default the source <c>Id</c> and <c>IdempotentKey</c> are dropped so the result is a genuinely
    /// new dialog; pass <paramref name="preserveId"/> as <see langword="true"/> to carry them over for an
    /// idempotent re-create. Read-only server fields on the source cannot be recovered and are dropped.
    /// The system label is taken from the end-user context (the category label Default/Bin/Archive, defaulting
    /// to Default), not the obsolete top-level <c>Dialog.SystemLabel</c>.
    /// </summary>
    /// <param name="source">The dialog to clone.</param>
    /// <param name="preserveId">When <see langword="true"/>, carries over <c>Id</c> and <c>IdempotentKey</c>.</param>
    public static CreateDialog ToCreateDialog(this Dialog source, bool preserveId = false) => new()
    {
        Id = preserveId ? source.Id : null,
        IdempotentKey = preserveId ? source.IdempotentKey : null,
        ServiceResource = source.ServiceResource,
        Party = source.Party,
        Progress = source.Progress,
        ExtendedStatus = source.ExtendedStatus,
        ExternalReference = source.ExternalReference,
        VisibleFrom = source.VisibleFrom,
        DueAt = source.DueAt,
        Process = source.Process,
        PrecedingProcess = source.PrecedingProcess,
        ExpiresAt = source.ExpiresAt,
        IsApiOnly = source.IsApiOnly,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        Status = source.Status.ToDialogStatusInput(),
        SystemLabel = source.EndUserContext.ToSystemLabel(),
        ServiceOwnerContext = source.ServiceOwnerContext.ToCreateDialogServiceOwnerContext(),
        Content = source.Content?.ToCreateDialogContent(),
        SearchTags = source.SearchTags?.Select(x => x.ToCreateDialogTag()).ToList(),
        Attachments = source.Attachments?.Select(x => x.ToCreateDialogAttachment()).ToList(),
        Transmissions = source.Transmissions?.Select(x => x.ToCreateDialogTransmission()).ToList(),
        GuiActions = source.GuiActions?.Select(x => x.ToCreateDialogGuiAction()).ToList(),
        ApiActions = source.ApiActions?.Select(x => x.ToCreateDialogApiAction()).ToList(),
        Activities = source.Activities?.Select(x => x.ToCreateDialogActivity()).ToList(),
    };

    /// <summary>
    /// Maps a <see cref="CreateDialog"/> to an <see cref="UpdateDialog"/>, reusing a create payload for an
    /// update. Create-only fields (identity, party, visibility, created/updated timestamps, system label and
    /// service-owner context) are dropped. A missing status defaults to <see cref="DialogStatusInput.New"/>.
    /// </summary>
    public static UpdateDialog ToUpdateDialog(this CreateDialog source) => new()
    {
        Progress = source.Progress,
        ExtendedStatus = source.ExtendedStatus,
        ExternalReference = source.ExternalReference,
        DueAt = source.DueAt,
        Process = source.Process,
        PrecedingProcess = source.PrecedingProcess,
        ExpiresAt = source.ExpiresAt,
        IsApiOnly = source.IsApiOnly,
        Status = source.Status ?? DialogStatusInput.New,
        Content = source.Content?.ToUpdateDialogContent(),
        SearchTags = source.SearchTags?.Select(x => x.ToUpdateDialogTag()).ToList(),
        Attachments = source.Attachments?.Select(x => x.ToUpdateDialogAttachment()).ToList(),
        Transmissions = source.Transmissions?.Select(x => x.ToUpdateDialogTransmission()).ToList(),
        GuiActions = source.GuiActions?.Select(x => x.ToUpdateDialogGuiAction()).ToList(),
        ApiActions = source.ApiActions?.Select(x => x.ToUpdateDialogApiAction()).ToList(),
        Activities = source.Activities?.Select(x => x.ToUpdateDialogActivity()).ToList(),
    };

    /// <summary>
    /// Maps an <see cref="UpdateDialog"/> to a <see cref="CreateDialog"/>. The create-only required fields
    /// <c>ServiceResource</c> and <c>Party</c> have no source on an update body and must be set by the caller
    /// on the result before it can be posted.
    /// </summary>
    public static CreateDialog ToCreateDialog(this UpdateDialog source) => new()
    {
        // ServiceResource and Party have no source on an update body; the caller must set these
        // required fields on the result before it can be posted (see remarks above).
        ServiceResource = null!,
        Party = null!,
        Progress = source.Progress,
        ExtendedStatus = source.ExtendedStatus,
        ExternalReference = source.ExternalReference,
        DueAt = source.DueAt,
        Process = source.Process,
        PrecedingProcess = source.PrecedingProcess,
        ExpiresAt = source.ExpiresAt,
        IsApiOnly = source.IsApiOnly,
        Status = source.Status,
        Content = source.Content?.ToCreateDialogContent(),
        SearchTags = source.SearchTags?.Select(x => x.ToCreateDialogTag()).ToList(),
        Attachments = source.Attachments?.Select(x => x.ToCreateDialogAttachment()).ToList(),
        Transmissions = source.Transmissions?.Select(x => x.ToCreateDialogTransmission()).ToList(),
        GuiActions = source.GuiActions?.Select(x => x.ToCreateDialogGuiAction()).ToList(),
        ApiActions = source.ApiActions?.Select(x => x.ToCreateDialogApiAction()).ToList(),
        Activities = source.Activities?.Select(x => x.ToCreateDialogActivity()).ToList(),
    };

    // Search tags

    private static CreateDialogTag ToCreateDialogTag(this DialogTag source) => new() { Value = source.Value };

    private static UpdateDialogTag ToUpdateDialogTag(this DialogTag source) => new() { Value = source.Value };

    private static UpdateDialogTag ToUpdateDialogTag(this CreateDialogTag source) => new() { Value = source.Value };

    private static CreateDialogTag ToCreateDialogTag(this UpdateDialogTag source) => new() { Value = source.Value };

    // System label: pick the category label (Default/Bin/Archive) from the end-user context, defaulting to
    // Default. The obsolete top-level Dialog.SystemLabel is intentionally not used.
    private static SystemLabel ToSystemLabel(this DialogEndUserContext source) =>
        source.SystemLabels?.FirstOrDefault(x => x is SystemLabel.Default or SystemLabel.Bin or SystemLabel.Archive)
     ?? SystemLabel.Default;

    // Service owner context / labels (create-only; the update body has no service-owner context)

    private static CreateDialogServiceOwnerContext ToCreateDialogServiceOwnerContext(this DialogServiceOwnerContext source) => new()
    {
        ServiceOwnerLabels = source.ServiceOwnerLabels?.Select(x => x.ToCreateDialogServiceOwnerLabel()).ToList(),
    };

    private static CreateDialogServiceOwnerLabel ToCreateDialogServiceOwnerLabel(this DialogServiceOwnerLabel source) => new()
    {
        Value = source.Value,
    };
}
