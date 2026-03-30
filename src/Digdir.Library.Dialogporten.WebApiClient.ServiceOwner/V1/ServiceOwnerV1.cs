using Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner;
using Refit;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

internal sealed class ServiceOwnerV1(
    IDialogsPostEndpoint dialogsPost,
    IDialogsGet2Endpoint dialogsGet,
    IDialogsGetEndpoint dialogsList,
    IDialogsPutEndpoint dialogsPut,
    IDialogsPatchEndpoint dialogsPatch,
    IDialogsDeleteEndpoint dialogsDelete,
    IRestoreEndpoint restore,
    IPurgeEndpoint purge,
    IFreezeEndpoint freeze,
    ITransmissionsPostEndpoint transmissionsPost,
    ITransmissionsGet2Endpoint transmissionsGet,
    ITransmissionsGetEndpoint transmissionsList,
    ITransmissionsPutEndpoint transmissionsPut,
    IActivitiesPostEndpoint activitiesPost,
    IActivitiesGet2Endpoint activitiesGet,
    IActivitiesGetEndpoint activitiesList,
    ILabelsGetEndpoint labelsGet,
    ILabelsPostEndpoint labelsPost,
    ILabelsDeleteEndpoint labelsDelete,
    ISystemlabelsEndpoint systemLabels,
    IBulksetEndpoint bulkSetSystemLabels,
    ISeenlogGet2Endpoint seenLogGet,
    ISeenlogGetEndpoint seenLogList,
    IEndusercontextEndpoint endUserContext,
    IDialoglookupEndpoint dialogLookup,
    IShouldSendNotificationEndpoint notificationCondition)
    : IServiceOwnerV1
{
    #region Dialogs

    public Task<IApiResponse<string>> CreateDialogAsync(
        V1ServiceOwnerDialogsCommandsCreate_Dialog request,
        CancellationToken cancellationToken) =>
        dialogsPost.Execute(request, cancellationToken);

    public Task<IApiResponse<V1ServiceOwnerDialogsQueriesGet_Dialog>> GetDialogAsync(Guid dialogId, string? endUserId, CancellationToken cancellationToken) =>
        dialogsGet.Execute(dialogId, endUserId!, cancellationToken);

    public Task<IApiResponse<PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog>> ListDialogsAsync(DialogsGetQueryParams? queryParams, CancellationToken cancellationToken) =>
        dialogsList.Execute(queryParams ?? new DialogsGetQueryParams(), cancellationToken);

    public Task<IApiResponse> UpdateDialogAsync(Guid dialogId, V1ServiceOwnerDialogsCommandsUpdate_Dialog request, Guid? revision, CancellationToken cancellationToken) =>
        dialogsPut.Execute(dialogId, request, revision, cancellationToken);

    public Task<IApiResponse> PatchDialogAsync(Guid dialogId, IEnumerable<JsonPatchOperations_Operation> patchDocument, Guid? revision, CancellationToken cancellationToken) =>
        dialogsPatch.Execute(dialogId, patchDocument, revision, cancellationToken);

    public Task<IApiResponse> DeleteDialogAsync(Guid dialogId, Guid? revision, CancellationToken cancellationToken) =>
        dialogsDelete.Execute(dialogId, revision, cancellationToken);

    public Task<IApiResponse> RestoreDialogAsync(Guid dialogId, Guid? revision, CancellationToken cancellationToken) =>
        restore.Execute(dialogId, revision, cancellationToken);

    public Task<IApiResponse> PurgeDialogAsync(Guid dialogId, Guid? revision, CancellationToken cancellationToken) =>
        purge.Execute(dialogId, revision, cancellationToken);

    public Task<IApiResponse> FreezeDialogAsync(Guid dialogId, Guid? revision, CancellationToken cancellationToken) =>
        freeze.Execute(dialogId, revision, cancellationToken);

    #endregion

    #region Transmissions

    public Task<IApiResponse<string>> CreateTransmissionAsync(Guid dialogId, V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest request, Guid? revision, CancellationToken cancellationToken) =>
        transmissionsPost.Execute(dialogId, request, revision, cancellationToken);

    public Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetTransmission_Transmission>> GetTransmissionAsync(Guid dialogId, Guid transmissionId, CancellationToken cancellationToken) =>
        transmissionsGet.Execute(dialogId, transmissionId, cancellationToken);

    public Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchTransmissions_Transmission>>> ListTransmissionsAsync(Guid dialogId, CancellationToken cancellationToken) =>
        transmissionsList.Execute(dialogId, cancellationToken);

    public Task<IApiResponse> UpdateTransmissionAsync(Guid dialogId, Guid transmissionId, V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest request, Guid? revision, CancellationToken cancellationToken) =>
        transmissionsPut.Execute(dialogId, transmissionId, request, revision, cancellationToken);

    #endregion

    #region Activities

    public Task<IApiResponse<string>> CreateActivityAsync(Guid dialogId, V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest request, Guid? revision, CancellationToken cancellationToken) =>
        activitiesPost.Execute(dialogId, request, revision, cancellationToken);

    public Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetActivity_Activity>> GetActivityAsync(Guid dialogId, Guid activityId, CancellationToken cancellationToken) =>
        activitiesGet.Execute(dialogId, activityId, cancellationToken);

    public Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchActivities_Activity>>> ListActivitiesAsync(Guid dialogId, CancellationToken cancellationToken) =>
        activitiesList.Execute(dialogId, cancellationToken);

    #endregion

    #region Service Owner Labels

    public Task<IApiResponse> GetServiceOwnerLabelsAsync(Guid dialogId, CancellationToken cancellationToken) =>
        labelsGet.Execute(dialogId, cancellationToken);

    public Task<IApiResponse> AddServiceOwnerLabelAsync(Guid dialogId, V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabel_Label label, Guid? revision, CancellationToken cancellationToken) =>
        labelsPost.Execute(dialogId, label, revision, cancellationToken);

    public Task<IApiResponse> DeleteServiceOwnerLabelAsync(Guid dialogId, string label, Guid? revision, CancellationToken cancellationToken) =>
        labelsDelete.Execute(dialogId, label, revision, cancellationToken);

    #endregion

    #region System Labels

    public Task<IApiResponse> SetSystemLabelAsync(Guid dialogId, string endUserId, V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest request, Guid? revision, CancellationToken cancellationToken) =>
        systemLabels.Execute(dialogId, endUserId, request, revision, cancellationToken);

    public Task<IApiResponse> BulkSetSystemLabelsAsync(string endUserId, V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel request, CancellationToken cancellationToken) =>
        bulkSetSystemLabels.Execute(endUserId, request, cancellationToken);

    #endregion

    #region Seen Logs

    public Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetSeenLog_SeenLog>> GetSeenLogAsync(Guid dialogId, Guid seenLogId, CancellationToken cancellationToken) =>
        seenLogGet.Execute(dialogId, seenLogId, cancellationToken);

    public Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchSeenLogs_SeenLog>>> ListSeenLogsAsync(Guid dialogId, CancellationToken cancellationToken) =>
        seenLogList.Execute(dialogId, cancellationToken);

    #endregion

    #region End User Context

    public Task<IApiResponse<PaginatedListOfV1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem>> ListEndUserContextAsync(EndusercontextQueryParams queryParams, CancellationToken cancellationToken) =>
        endUserContext.Execute(queryParams, cancellationToken);

    #endregion

    #region Dialog Lookup

    public Task<IApiResponse<V1CommonIdentifierLookup_ServiceOwnerIdentifierLookup>> GetDialogLookupAsync(string instanceRef, V1EndUserCommon_AcceptedLanguages? acceptLanguage, CancellationToken cancellationToken) =>
        dialogLookup.Execute(instanceRef, acceptLanguage ?? new V1EndUserCommon_AcceptedLanguages(), cancellationToken);

    #endregion

    #region Notification Condition

    public Task<IApiResponse<V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationCondition>> GetNotificationConditionAsync(Guid dialogId, ShouldSendNotificationQueryParams queryParams, CancellationToken cancellationToken) =>
        notificationCondition.Execute(dialogId, queryParams, cancellationToken);

    #endregion
}
