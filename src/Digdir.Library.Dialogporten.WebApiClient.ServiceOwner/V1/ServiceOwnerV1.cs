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
        CreateDialogRequest request,
        CancellationToken cancellationToken) =>
        dialogsPost.Execute(request.ToTransport(), cancellationToken);

    public async Task<IApiResponse<Dialog>> GetDialogAsync(Guid dialogId, string? endUserId, CancellationToken cancellationToken) =>
        (await dialogsGet.Execute(dialogId, endUserId!, cancellationToken))
            .MapContent(static dialog => dialog.ToContract());

    public async Task<IApiResponse<PaginatedDialogList>> ListDialogsAsync(SearchDialogQueryParams? queryParams, CancellationToken cancellationToken) =>
        (await dialogsList.Execute(queryParams?.ToTransport() ?? new DialogsGetQueryParams(), cancellationToken))
            .MapContent(static dialogs => dialogs.ToContract());

    public Task<IApiResponse> UpdateDialogAsync(Guid dialogId, UpdateDialogRequest request, Guid? revision, CancellationToken cancellationToken) =>
        dialogsPut.Execute(dialogId, request.ToTransport(), revision, cancellationToken);

    public Task<IApiResponse> PatchDialogAsync(Guid dialogId, IEnumerable<PatchOperation> patchDocument, Guid? revision, CancellationToken cancellationToken) =>
        dialogsPatch.Execute(dialogId, patchDocument.Select(static operation => operation.ToTransport()).ToList(), revision, cancellationToken);

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

    public Task<IApiResponse<string>> CreateTransmissionAsync(Guid dialogId, CreateTransmissionRequest request, Guid? revision, CancellationToken cancellationToken) =>
        transmissionsPost.Execute(dialogId, request.ToTransport(), revision, cancellationToken);

    public async Task<IApiResponse<Transmission>> GetTransmissionAsync(Guid dialogId, Guid transmissionId, CancellationToken cancellationToken) =>
        (await transmissionsGet.Execute(dialogId, transmissionId, cancellationToken))
            .MapContent(static transmission => transmission.ToContract());

    public async Task<IApiResponse<ICollection<TransmissionSummary>>> ListTransmissionsAsync(Guid dialogId, CancellationToken cancellationToken) =>
        (await transmissionsList.Execute(dialogId, cancellationToken))
            .MapContent(static transmissions => transmissions.Select(static transmission => transmission.ToContract()).ToList());

    public Task<IApiResponse> UpdateTransmissionAsync(Guid dialogId, Guid transmissionId, UpdateTransmissionRequest request, Guid? revision, CancellationToken cancellationToken) =>
        transmissionsPut.Execute(dialogId, transmissionId, request.ToTransport(), revision, cancellationToken);

    #endregion

    #region Activities

    public Task<IApiResponse<string>> CreateActivityAsync(Guid dialogId, CreateActivityRequest request, Guid? revision, CancellationToken cancellationToken) =>
        activitiesPost.Execute(dialogId, request.ToTransport(), revision, cancellationToken);

    public async Task<IApiResponse<Activity>> GetActivityAsync(Guid dialogId, Guid activityId, CancellationToken cancellationToken) =>
        (await activitiesGet.Execute(dialogId, activityId, cancellationToken))
            .MapContent(static activity => activity.ToContract());

    public async Task<IApiResponse<ICollection<ActivitySummary>>> ListActivitiesAsync(Guid dialogId, CancellationToken cancellationToken) =>
        (await activitiesList.Execute(dialogId, cancellationToken))
            .MapContent(static activities => activities.Select(static activity => activity.ToContract()).ToList());

    #endregion

    #region Service Owner Labels

    public Task<IApiResponse> GetServiceOwnerLabelsAsync(Guid dialogId, CancellationToken cancellationToken) =>
        labelsGet.Execute(dialogId, cancellationToken);

    public Task<IApiResponse> AddServiceOwnerLabelAsync(Guid dialogId, CreateServiceOwnerLabelRequest label, Guid? revision, CancellationToken cancellationToken) =>
        labelsPost.Execute(dialogId, label.ToTransport(), revision, cancellationToken);

    public Task<IApiResponse> DeleteServiceOwnerLabelAsync(Guid dialogId, string label, Guid? revision, CancellationToken cancellationToken) =>
        labelsDelete.Execute(dialogId, label, revision, cancellationToken);

    #endregion

    #region System Labels

    public Task<IApiResponse> SetSystemLabelAsync(Guid dialogId, string endUserId, SetSystemLabelRequest request, Guid? revision, CancellationToken cancellationToken) =>
        systemLabels.Execute(dialogId, endUserId, request.ToTransport(), revision, cancellationToken);

    public Task<IApiResponse> BulkSetSystemLabelsAsync(string endUserId, BulkSetSystemLabelsRequest request, CancellationToken cancellationToken) =>
        bulkSetSystemLabels.Execute(endUserId, request.ToTransport(), cancellationToken);

    #endregion

    #region Seen Logs

    public async Task<IApiResponse<SeenLog>> GetSeenLogAsync(Guid dialogId, Guid seenLogId, CancellationToken cancellationToken) =>
        (await seenLogGet.Execute(dialogId, seenLogId, cancellationToken))
            .MapContent(static seenLog => seenLog.ToContract());

    public async Task<IApiResponse<ICollection<SeenLogSummary>>> ListSeenLogsAsync(Guid dialogId, CancellationToken cancellationToken) =>
        (await seenLogList.Execute(dialogId, cancellationToken))
            .MapContent(static seenLogs => seenLogs.Select(static seenLog => seenLog.ToContract()).ToList());

    #endregion

    #region End User Context

    public async Task<IApiResponse<PaginatedEndUserContextList>> ListEndUserContextAsync(SearchEndUserContextQueryParams queryParams, CancellationToken cancellationToken) =>
        (await endUserContext.Execute(queryParams.ToTransport(), cancellationToken))
            .MapContent(static endUserContextResult => endUserContextResult.ToContract());

    #endregion

    #region Dialog Lookup

    public async Task<IApiResponse<DialogLookup>> GetDialogLookupAsync(string instanceRef, AcceptedLanguages? acceptLanguage, CancellationToken cancellationToken) =>
        (await dialogLookup.Execute(instanceRef, acceptLanguage?.ToTransport() ?? new V1EndUserCommon_AcceptedLanguages(), cancellationToken))
            .MapContent(static dialogLookupResponse => dialogLookupResponse.ToContract());

    #endregion

    #region Notification Condition

    public async Task<IApiResponse<NotificationCondition>> GetNotificationConditionAsync(Guid dialogId, NotificationConditionQueryParams queryParams, CancellationToken cancellationToken) =>
        (await notificationCondition.Execute(dialogId, queryParams.ToTransport(), cancellationToken))
            .MapContent(static condition => condition.ToContract());

    #endregion
}
