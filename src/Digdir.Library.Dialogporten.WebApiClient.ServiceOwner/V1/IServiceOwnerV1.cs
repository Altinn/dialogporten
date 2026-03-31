using Refit;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

/// <summary>
/// Service owner operations for the Dialogporten V1 API.
/// </summary>
public interface IServiceOwnerV1
{
    #region Dialogs

    /// <summary>Creates a new dialog.</summary>
    /// <returns>Response containing the location/ID of the created dialog.</returns>
    Task<IApiResponse<string>> CreateDialog(CreateDialogRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a dialog by ID.</summary>
    Task<IApiResponse<Dialog>> GetDialog(Guid dialogId, string? endUserId = null, CancellationToken cancellationToken = default);

    /// <summary>Searches dialogs with optional filters.</summary>
    Task<IApiResponse<PaginatedDialogList>> ListDialogs(SearchDialogQueryParams? queryParams = null, CancellationToken cancellationToken = default);

    /// <summary>Updates a dialog (full replace).</summary>
    Task<IApiResponse> UpdateDialog(Guid dialogId, UpdateDialogRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Patches a dialog using JSON Patch operations.</summary>
    Task<IApiResponse> PatchDialog(Guid dialogId, IEnumerable<PatchOperation> patchDocument, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a dialog.</summary>
    Task<IApiResponse> DeleteDialog(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Restores a soft-deleted dialog.</summary>
    Task<IApiResponse> RestoreDialog(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Permanently purges a dialog.</summary>
    Task<IApiResponse> PurgeDialog(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Freezes a dialog, preventing further modifications.</summary>
    Task<IApiResponse> FreezeDialog(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region Transmissions

    /// <summary>Creates a new transmission on a dialog.</summary>
    Task<IApiResponse<string>> CreateTransmission(Guid dialogId, CreateTransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a specific transmission.</summary>
    Task<IApiResponse<Transmission>> GetTransmission(Guid dialogId, Guid transmissionId, CancellationToken cancellationToken = default);

    /// <summary>Lists all transmissions for a dialog.</summary>
    Task<IApiResponse<ICollection<TransmissionSummary>>> ListTransmissions(Guid dialogId, CancellationToken cancellationToken = default);

    /// <summary>Updates a transmission.</summary>
    Task<IApiResponse> UpdateTransmission(Guid dialogId, Guid transmissionId, UpdateTransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region Activities

    /// <summary>Creates a new activity on a dialog.</summary>
    Task<IApiResponse<string>> CreateActivity(Guid dialogId, CreateActivityRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a specific activity.</summary>
    Task<IApiResponse<Activity>> GetActivity(Guid dialogId, Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>Lists all activities for a dialog.</summary>
    Task<IApiResponse<ICollection<ActivitySummary>>> ListActivities(Guid dialogId, CancellationToken cancellationToken = default);

    #endregion

    #region Service Owner Labels

    /// <summary>Gets service owner labels for a dialog.</summary>
    Task<IApiResponse> GetServiceOwnerLabels(Guid dialogId, CancellationToken cancellationToken = default);

    /// <summary>Adds a service owner label to a dialog.</summary>
    Task<IApiResponse> AddServiceOwnerLabel(Guid dialogId, CreateServiceOwnerLabelRequest label, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes a service owner label from a dialog.</summary>
    Task<IApiResponse> DeleteServiceOwnerLabel(Guid dialogId, string label, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region System Labels

    /// <summary>Sets system labels for a dialog's end user context.</summary>
    Task<IApiResponse> SetSystemLabel(Guid dialogId, string endUserId, SetSystemLabelRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Bulk sets system labels across multiple dialogs.</summary>
    Task<IApiResponse> BulkSetSystemLabels(string endUserId, BulkSetSystemLabelsRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Seen Logs

    /// <summary>Gets a specific seen log entry.</summary>
    Task<IApiResponse<SeenLog>> GetSeenLog(Guid dialogId, Guid seenLogId, CancellationToken cancellationToken = default);

    /// <summary>Lists seen log entries for a dialog.</summary>
    Task<IApiResponse<ICollection<SeenLogSummary>>> ListSeenLogs(Guid dialogId, CancellationToken cancellationToken = default);

    #endregion

    #region End User Context

    /// <summary>Searches end user context across dialogs.</summary>
    Task<IApiResponse<PaginatedEndUserContextList>> ListEndUserContext(SearchEndUserContextQueryParams queryParams, CancellationToken cancellationToken = default);

    #endregion

    #region Dialog Lookup

    /// <summary>Looks up a dialog by instance reference.</summary>
    Task<IApiResponse<DialogLookup>> GetDialogLookup(string instanceRef, AcceptedLanguages? acceptLanguage = null, CancellationToken cancellationToken = default);

    #endregion

    #region Notification Condition

    /// <summary>Checks whether a notification should be sent for a dialog.</summary>
    Task<IApiResponse<NotificationCondition>> GetNotificationCondition(Guid dialogId, NotificationConditionQueryParams queryParams, CancellationToken cancellationToken = default);

    #endregion
}
