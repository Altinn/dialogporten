using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

/// <summary>
/// Maps the dialog activity hierarchies. The <c>PerformedBy</c> actor and the localized description are
/// shared types and are reused by reference.
/// </summary>
internal static class ActivityMappingExtensions
{
    internal static CreateDialogActivity ToCreateDialogActivity(this DialogActivity source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description,
    };

    internal static UpdateDialogActivity ToUpdateDialogActivity(this DialogActivity source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description,
    };

    internal static UpdateDialogActivity ToUpdateDialogActivity(this CreateDialogActivity source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description,
    };

    internal static CreateDialogActivity ToCreateDialogActivity(this UpdateDialogActivity source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description,
    };

    // Create/Update -> Get. The Get Id is non-null; the optional input Id is defaulted to Guid.Empty when absent.

    internal static DialogActivity ToDialogActivity(this CreateDialogActivity source) => new()
    {
        Id = source.Id ?? default,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description,
    };

    internal static DialogActivity ToDialogActivity(this UpdateDialogActivity source) => new()
    {
        Id = source.Id ?? default,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description,
    };
}
