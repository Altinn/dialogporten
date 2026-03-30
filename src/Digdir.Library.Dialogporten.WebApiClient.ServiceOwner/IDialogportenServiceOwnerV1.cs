using Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <summary>
/// Service owner operations for the Dialogporten V1 API.
/// </summary>
public interface IDialogportenServiceOwnerV1
{
    #region Dialogs

    /// <summary>Creates a new dialog.</summary>
    /// <returns>The ID of the created dialog.</returns>
    Task<string> CreateDialogAsync(CreateDialogRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a dialog by ID.</summary>
    /// <param name="dialogId">The dialog ID.</param>
    /// <param name="endUserId">Optional end user ID for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Dialog> GetDialogAsync(Guid dialogId, string? endUserId = null, CancellationToken cancellationToken = default);

    /// <summary>Searches dialogs with optional filters.</summary>
    Task<PaginatedDialogList> ListDialogsAsync(SearchDialogQueryParams? queryParams = null, CancellationToken cancellationToken = default);

    /// <summary>Updates a dialog (full replace).</summary>
    Task UpdateDialogAsync(Guid dialogId, UpdateDialogRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Patches a dialog using JSON Patch operations.</summary>
    Task PatchDialogAsync(Guid dialogId, IEnumerable<PatchOperation> patchDocument, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a dialog.</summary>
    Task DeleteDialogAsync(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Restores a soft-deleted dialog.</summary>
    Task RestoreDialogAsync(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Permanently purges a dialog.</summary>
    Task PurgeDialogAsync(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Freezes a dialog, preventing further modifications.</summary>
    Task FreezeDialogAsync(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region Transmissions

    /// <summary>Creates a new transmission on a dialog.</summary>
    /// <returns>The ID of the created transmission.</returns>
    Task<string> CreateTransmissionAsync(Guid dialogId, CreateTransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a specific transmission.</summary>
    Task<Transmission> GetTransmissionAsync(Guid dialogId, Guid transmissionId, CancellationToken cancellationToken = default);

    /// <summary>Lists all transmissions for a dialog.</summary>
    Task<ICollection<TransmissionSummary>> ListTransmissionsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    /// <summary>Updates a transmission.</summary>
    Task UpdateTransmissionAsync(Guid dialogId, Guid transmissionId, UpdateTransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region Activities

    /// <summary>Creates a new activity on a dialog.</summary>
    /// <returns>The ID of the created activity.</returns>
    Task<string> CreateActivityAsync(Guid dialogId, CreateActivityRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a specific activity.</summary>
    Task<Activity> GetActivityAsync(Guid dialogId, Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>Lists all activities for a dialog.</summary>
    Task<ICollection<ActivitySummary>> ListActivitiesAsync(Guid dialogId, CancellationToken cancellationToken = default);

    #endregion

    #region Service Owner Labels

    /// <summary>Gets service owner labels for a dialog.</summary>
    Task GetServiceOwnerLabelsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    /// <summary>Adds a service owner label to a dialog.</summary>
    Task AddServiceOwnerLabelAsync(Guid dialogId, CreateServiceOwnerLabelRequest label, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes a service owner label from a dialog.</summary>
    Task DeleteServiceOwnerLabelAsync(Guid dialogId, string label, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region System Labels

    /// <summary>Sets system labels for a dialog's end user context.</summary>
    Task SetSystemLabelAsync(Guid dialogId, string endUserId, SetSystemLabelRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Bulk sets system labels across multiple dialogs.</summary>
    Task BulkSetSystemLabelsAsync(string endUserId, BulkSetSystemLabelsRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Seen Logs

    /// <summary>Gets a specific seen log entry.</summary>
    Task<SeenLog> GetSeenLogAsync(Guid dialogId, Guid seenLogId, CancellationToken cancellationToken = default);

    /// <summary>Lists seen log entries for a dialog.</summary>
    Task<ICollection<SeenLogSummary>> ListSeenLogsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    #endregion

    #region End User Context

    /// <summary>Searches end user context across dialogs.</summary>
    Task<PaginatedEndUserContextList> ListEndUserContextAsync(SearchEndUserContextQueryParams queryParams, CancellationToken cancellationToken = default);

    #endregion

    #region Dialog Lookup

    /// <summary>Looks up a dialog by instance reference.</summary>
    Task<DialogLookup> GetDialogLookupAsync(string instanceRef, V1EndUserCommon_AcceptedLanguages? acceptLanguage = null, CancellationToken cancellationToken = default);

    #endregion

    #region Notification Condition

    /// <summary>Checks whether a notification should be sent for a dialog.</summary>
    Task<NotificationCondition> GetNotificationConditionAsync(Guid dialogId, NotificationConditionQueryParams queryParams, CancellationToken cancellationToken = default);

    #endregion
}
