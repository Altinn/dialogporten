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
    Task<IApiResponse<string>> CreateDialogAsync(CreateDialogRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a dialog by ID.</summary>
    Task<IApiResponse<Dialog>> GetDialogAsync(Guid dialogId, string? endUserId = null, CancellationToken cancellationToken = default);

    /// <summary>Searches dialogs with optional filters.</summary>
    Task<IApiResponse<PaginatedDialogList>> ListDialogsAsync(SearchDialogQueryParams? queryParams = null, CancellationToken cancellationToken = default);

    /// <summary>Updates a dialog (full replace).</summary>
    Task<IApiResponse> UpdateDialogAsync(Guid dialogId, UpdateDialogRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Patches a dialog using JSON Patch operations.</summary>
    Task<IApiResponse> PatchDialogAsync(Guid dialogId, IEnumerable<PatchOperation> patchDocument, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a dialog.</summary>
    Task<IApiResponse> DeleteDialogAsync(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Restores a soft-deleted dialog.</summary>
    Task<IApiResponse> RestoreDialogAsync(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Permanently purges a dialog.</summary>
    Task<IApiResponse> PurgeDialogAsync(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Freezes a dialog, preventing further modifications.</summary>
    Task<IApiResponse> FreezeDialogAsync(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region Transmissions

    /// <summary>Creates a new transmission on a dialog.</summary>
    Task<IApiResponse<string>> CreateTransmissionAsync(Guid dialogId, CreateTransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a specific transmission.</summary>
    Task<IApiResponse<Transmission>> GetTransmissionAsync(Guid dialogId, Guid transmissionId, CancellationToken cancellationToken = default);

    /// <summary>Lists all transmissions for a dialog.</summary>
    Task<IApiResponse<ICollection<TransmissionSummary>>> ListTransmissionsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    /// <summary>Updates a transmission.</summary>
    Task<IApiResponse> UpdateTransmissionAsync(Guid dialogId, Guid transmissionId, UpdateTransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region Activities

    /// <summary>Creates a new activity on a dialog.</summary>
    Task<IApiResponse<string>> CreateActivityAsync(Guid dialogId, CreateActivityRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a specific activity.</summary>
    Task<IApiResponse<Activity>> GetActivityAsync(Guid dialogId, Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>Lists all activities for a dialog.</summary>
    Task<IApiResponse<ICollection<ActivitySummary>>> ListActivitiesAsync(Guid dialogId, CancellationToken cancellationToken = default);

    #endregion

    #region Service Owner Labels

    /// <summary>Gets service owner labels for a dialog.</summary>
    Task<IApiResponse> GetServiceOwnerLabelsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    /// <summary>Adds a service owner label to a dialog.</summary>
    Task<IApiResponse> AddServiceOwnerLabelAsync(Guid dialogId, CreateServiceOwnerLabelRequest label, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes a service owner label from a dialog.</summary>
    Task<IApiResponse> DeleteServiceOwnerLabelAsync(Guid dialogId, string label, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region System Labels

    /// <summary>Sets system labels for a dialog's end user context.</summary>
    Task<IApiResponse> SetSystemLabelAsync(Guid dialogId, string endUserId, SetSystemLabelRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Bulk sets system labels across multiple dialogs.</summary>
    Task<IApiResponse> BulkSetSystemLabelsAsync(string endUserId, BulkSetSystemLabelsRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Seen Logs

    /// <summary>Gets a specific seen log entry.</summary>
    Task<IApiResponse<SeenLog>> GetSeenLogAsync(Guid dialogId, Guid seenLogId, CancellationToken cancellationToken = default);

    /// <summary>Lists seen log entries for a dialog.</summary>
    Task<IApiResponse<ICollection<SeenLogSummary>>> ListSeenLogsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    #endregion

    #region End User Context

    /// <summary>Searches end user context across dialogs.</summary>
    Task<IApiResponse<PaginatedEndUserContextList>> ListEndUserContextAsync(SearchEndUserContextQueryParams queryParams, CancellationToken cancellationToken = default);

    #endregion

    #region Dialog Lookup

    /// <summary>Looks up a dialog by instance reference.</summary>
    Task<IApiResponse<DialogLookup>> GetDialogLookupAsync(string instanceRef, AcceptedLanguages? acceptLanguage = null, CancellationToken cancellationToken = default);

    #endregion

    #region Notification Condition

    /// <summary>Checks whether a notification should be sent for a dialog.</summary>
    Task<IApiResponse<NotificationCondition>> GetNotificationConditionAsync(Guid dialogId, NotificationConditionQueryParams queryParams, CancellationToken cancellationToken = default);

    #endregion
}
