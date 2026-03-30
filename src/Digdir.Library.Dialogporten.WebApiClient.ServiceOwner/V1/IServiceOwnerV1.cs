using Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner;
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
    Task<IApiResponse<string>> CreateDialogAsync(V1ServiceOwnerDialogsCommandsCreate_Dialog request, CancellationToken cancellationToken = default);

    /// <summary>Gets a dialog by ID.</summary>
    Task<IApiResponse<V1ServiceOwnerDialogsQueriesGet_Dialog>> GetDialogAsync(Guid dialogId, string? endUserId = null, CancellationToken cancellationToken = default);

    /// <summary>Searches dialogs with optional filters.</summary>
    Task<IApiResponse<PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog>> ListDialogsAsync(DialogsGetQueryParams? queryParams = null, CancellationToken cancellationToken = default);

    /// <summary>Updates a dialog (full replace).</summary>
    Task<IApiResponse> UpdateDialogAsync(Guid dialogId, V1ServiceOwnerDialogsCommandsUpdate_Dialog request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Patches a dialog using JSON Patch operations.</summary>
    Task<IApiResponse> PatchDialogAsync(Guid dialogId, IEnumerable<JsonPatchOperations_Operation> patchDocument, Guid? revision = null, CancellationToken cancellationToken = default);

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
    Task<IApiResponse<string>> CreateTransmissionAsync(Guid dialogId, V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a specific transmission.</summary>
    Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetTransmission_Transmission>> GetTransmissionAsync(Guid dialogId, Guid transmissionId, CancellationToken cancellationToken = default);

    /// <summary>Lists all transmissions for a dialog.</summary>
    Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchTransmissions_Transmission>>> ListTransmissionsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    /// <summary>Updates a transmission.</summary>
    Task<IApiResponse> UpdateTransmissionAsync(Guid dialogId, Guid transmissionId, V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region Activities

    /// <summary>Creates a new activity on a dialog.</summary>
    Task<IApiResponse<string>> CreateActivityAsync(Guid dialogId, V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a specific activity.</summary>
    Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetActivity_Activity>> GetActivityAsync(Guid dialogId, Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>Lists all activities for a dialog.</summary>
    Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchActivities_Activity>>> ListActivitiesAsync(Guid dialogId, CancellationToken cancellationToken = default);

    #endregion

    #region Service Owner Labels

    /// <summary>Gets service owner labels for a dialog.</summary>
    Task<IApiResponse> GetServiceOwnerLabelsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    /// <summary>Adds a service owner label to a dialog.</summary>
    Task<IApiResponse> AddServiceOwnerLabelAsync(Guid dialogId, V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabel_Label label, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes a service owner label from a dialog.</summary>
    Task<IApiResponse> DeleteServiceOwnerLabelAsync(Guid dialogId, string label, Guid? revision = null, CancellationToken cancellationToken = default);

    #endregion

    #region System Labels

    /// <summary>Sets system labels for a dialog's end user context.</summary>
    Task<IApiResponse> SetSystemLabelAsync(Guid dialogId, string endUserId, V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest request, Guid? revision = null, CancellationToken cancellationToken = default);

    /// <summary>Bulk sets system labels across multiple dialogs.</summary>
    Task<IApiResponse> BulkSetSystemLabelsAsync(string endUserId, V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel request, CancellationToken cancellationToken = default);

    #endregion

    #region Seen Logs

    /// <summary>Gets a specific seen log entry.</summary>
    Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetSeenLog_SeenLog>> GetSeenLogAsync(Guid dialogId, Guid seenLogId, CancellationToken cancellationToken = default);

    /// <summary>Lists seen log entries for a dialog.</summary>
    Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchSeenLogs_SeenLog>>> ListSeenLogsAsync(Guid dialogId, CancellationToken cancellationToken = default);

    #endregion

    #region End User Context

    /// <summary>Searches end user context across dialogs.</summary>
    Task<IApiResponse<PaginatedListOfV1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem>> ListEndUserContextAsync(EndusercontextQueryParams queryParams, CancellationToken cancellationToken = default);

    #endregion

    #region Dialog Lookup

    /// <summary>Looks up a dialog by instance reference.</summary>
    Task<IApiResponse<V1CommonIdentifierLookup_ServiceOwnerIdentifierLookup>> GetDialogLookupAsync(string instanceRef, V1EndUserCommon_AcceptedLanguages? acceptLanguage = null, CancellationToken cancellationToken = default);

    #endregion

    #region Notification Condition

    /// <summary>Checks whether a notification should be sent for a dialog.</summary>
    Task<IApiResponse<V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationCondition>> GetNotificationConditionAsync(Guid dialogId, ShouldSendNotificationQueryParams queryParams, CancellationToken cancellationToken = default);

    #endregion
}
