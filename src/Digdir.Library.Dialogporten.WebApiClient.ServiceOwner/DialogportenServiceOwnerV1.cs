using Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

internal sealed class DialogportenServiceOwnerV1 : IDialogportenServiceOwnerV1
{
    private readonly IDialogsPostEndpoint _dialogsPost;
    private readonly IDialogsGet2Endpoint _dialogsGet;
    private readonly IDialogsGetEndpoint _dialogsList;
    private readonly IDialogsPutEndpoint _dialogsPut;
    private readonly IDialogsPatchEndpoint _dialogsPatch;
    private readonly IDialogsDeleteEndpoint _dialogsDelete;
    private readonly IRestoreEndpoint _restore;
    private readonly IPurgeEndpoint _purge;
    private readonly IFreezeEndpoint _freeze;
    private readonly ITransmissionsPostEndpoint _transmissionsPost;
    private readonly ITransmissionsGet2Endpoint _transmissionsGet;
    private readonly ITransmissionsGetEndpoint _transmissionsList;
    private readonly ITransmissionsPutEndpoint _transmissionsPut;
    private readonly IActivitiesPostEndpoint _activitiesPost;
    private readonly IActivitiesGet2Endpoint _activitiesGet;
    private readonly IActivitiesGetEndpoint _activitiesList;
    private readonly ILabelsGetEndpoint _labelsGet;
    private readonly ILabelsPostEndpoint _labelsPost;
    private readonly ILabelsDeleteEndpoint _labelsDelete;
    private readonly ISystemlabelsEndpoint _systemLabels;
    private readonly IBulksetEndpoint _bulkSetSystemLabels;
    private readonly ISeenlogGet2Endpoint _seenLogGet;
    private readonly ISeenlogGetEndpoint _seenLogList;
    private readonly IEndusercontextEndpoint _endUserContext;
    private readonly IDialoglookupEndpoint _dialogLookup;
    private readonly IShouldSendNotificationEndpoint _notificationCondition;

    public DialogportenServiceOwnerV1(
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
    {
        _dialogsPost = dialogsPost;
        _dialogsGet = dialogsGet;
        _dialogsList = dialogsList;
        _dialogsPut = dialogsPut;
        _dialogsPatch = dialogsPatch;
        _dialogsDelete = dialogsDelete;
        _restore = restore;
        _purge = purge;
        _freeze = freeze;
        _transmissionsPost = transmissionsPost;
        _transmissionsGet = transmissionsGet;
        _transmissionsList = transmissionsList;
        _transmissionsPut = transmissionsPut;
        _activitiesPost = activitiesPost;
        _activitiesGet = activitiesGet;
        _activitiesList = activitiesList;
        _labelsGet = labelsGet;
        _labelsPost = labelsPost;
        _labelsDelete = labelsDelete;
        _systemLabels = systemLabels;
        _bulkSetSystemLabels = bulkSetSystemLabels;
        _seenLogGet = seenLogGet;
        _seenLogList = seenLogList;
        _endUserContext = endUserContext;
        _dialogLookup = dialogLookup;
        _notificationCondition = notificationCondition;
    }

    #region Dialogs

    public async Task<string> CreateDialogAsync(CreateDialogRequest request, CancellationToken cancellationToken)
    {
        var response = await _dialogsPost.Execute(request, cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task<Dialog> GetDialogAsync(Guid dialogId, string? endUserId, CancellationToken cancellationToken)
    {
        var response = await _dialogsGet.Execute(dialogId, endUserId!, cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task<PaginatedDialogList> ListDialogsAsync(SearchDialogQueryParams? queryParams, CancellationToken cancellationToken)
    {
        var response = await _dialogsList.Execute(queryParams ?? new SearchDialogQueryParams(), cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task UpdateDialogAsync(Guid dialogId, UpdateDialogRequest request, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _dialogsPut.Execute(dialogId, request, revision, cancellationToken);
        response.EnsureSuccess();
    }

    public async Task PatchDialogAsync(Guid dialogId, IEnumerable<PatchOperation> patchDocument, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _dialogsPatch.Execute(dialogId, patchDocument, revision, cancellationToken);
        response.EnsureSuccess();
    }

    public async Task DeleteDialogAsync(Guid dialogId, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _dialogsDelete.Execute(dialogId, revision, cancellationToken);
        response.EnsureSuccess();
    }

    public async Task RestoreDialogAsync(Guid dialogId, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _restore.Execute(dialogId, revision, cancellationToken);
        response.EnsureSuccess();
    }

    public async Task PurgeDialogAsync(Guid dialogId, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _purge.Execute(dialogId, revision, cancellationToken);
        response.EnsureSuccess();
    }

    public async Task FreezeDialogAsync(Guid dialogId, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _freeze.Execute(dialogId, revision, cancellationToken);
        response.EnsureSuccess();
    }

    #endregion

    #region Transmissions

    public async Task<string> CreateTransmissionAsync(Guid dialogId, CreateTransmissionRequest request, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _transmissionsPost.Execute(dialogId, request, revision, cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task<Transmission> GetTransmissionAsync(Guid dialogId, Guid transmissionId, CancellationToken cancellationToken)
    {
        var response = await _transmissionsGet.Execute(dialogId, transmissionId, cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task<ICollection<TransmissionSummary>> ListTransmissionsAsync(Guid dialogId, CancellationToken cancellationToken)
    {
        var response = await _transmissionsList.Execute(dialogId, cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task UpdateTransmissionAsync(Guid dialogId, Guid transmissionId, UpdateTransmissionRequest request, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _transmissionsPut.Execute(dialogId, transmissionId, request, revision, cancellationToken);
        response.EnsureSuccess();
    }

    #endregion

    #region Activities

    public async Task<string> CreateActivityAsync(Guid dialogId, CreateActivityRequest request, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _activitiesPost.Execute(dialogId, request, revision, cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task<Activity> GetActivityAsync(Guid dialogId, Guid activityId, CancellationToken cancellationToken)
    {
        var response = await _activitiesGet.Execute(dialogId, activityId, cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task<ICollection<ActivitySummary>> ListActivitiesAsync(Guid dialogId, CancellationToken cancellationToken)
    {
        var response = await _activitiesList.Execute(dialogId, cancellationToken);
        return response.EnsureSuccess();
    }

    #endregion

    #region Service Owner Labels

    public async Task GetServiceOwnerLabelsAsync(Guid dialogId, CancellationToken cancellationToken)
    {
        var response = await _labelsGet.Execute(dialogId, cancellationToken);
        response.EnsureSuccess();
    }

    public async Task AddServiceOwnerLabelAsync(Guid dialogId, CreateServiceOwnerLabelRequest label, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _labelsPost.Execute(dialogId, label, revision, cancellationToken);
        response.EnsureSuccess();
    }

    public async Task DeleteServiceOwnerLabelAsync(Guid dialogId, string label, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _labelsDelete.Execute(dialogId, label, revision, cancellationToken);
        response.EnsureSuccess();
    }

    #endregion

    #region System Labels

    public async Task SetSystemLabelAsync(Guid dialogId, string endUserId, SetSystemLabelRequest request, Guid? revision, CancellationToken cancellationToken)
    {
        var response = await _systemLabels.Execute(dialogId, endUserId, request, revision, cancellationToken);
        response.EnsureSuccess();
    }

    public async Task BulkSetSystemLabelsAsync(string endUserId, BulkSetSystemLabelsRequest request, CancellationToken cancellationToken)
    {
        var response = await _bulkSetSystemLabels.Execute(endUserId, request, cancellationToken);
        response.EnsureSuccess();
    }

    #endregion

    #region Seen Logs

    public async Task<SeenLog> GetSeenLogAsync(Guid dialogId, Guid seenLogId, CancellationToken cancellationToken)
    {
        var response = await _seenLogGet.Execute(dialogId, seenLogId, cancellationToken);
        return response.EnsureSuccess();
    }

    public async Task<ICollection<SeenLogSummary>> ListSeenLogsAsync(Guid dialogId, CancellationToken cancellationToken)
    {
        var response = await _seenLogList.Execute(dialogId, cancellationToken);
        return response.EnsureSuccess();
    }

    #endregion

    #region End User Context

    public async Task<PaginatedEndUserContextList> ListEndUserContextAsync(SearchEndUserContextQueryParams queryParams, CancellationToken cancellationToken)
    {
        var response = await _endUserContext.Execute(queryParams, cancellationToken);
        return response.EnsureSuccess();
    }

    #endregion

    #region Dialog Lookup

    public async Task<DialogLookup> GetDialogLookupAsync(string instanceRef, V1EndUserCommon_AcceptedLanguages? acceptLanguage, CancellationToken cancellationToken)
    {
        var response = await _dialogLookup.Execute(instanceRef, acceptLanguage ?? new V1EndUserCommon_AcceptedLanguages(), cancellationToken);
        return response.EnsureSuccess();
    }

    #endregion

    #region Notification Condition

    public async Task<NotificationCondition> GetNotificationConditionAsync(Guid dialogId, NotificationConditionQueryParams queryParams, CancellationToken cancellationToken)
    {
        var response = await _notificationCondition.Execute(dialogId, queryParams, cancellationToken);
        return response.EnsureSuccess();
    }

    #endregion
}
