using Altinn.ApiClients.Dialogporten.Features.V1;
using Refit;

namespace Digdir.Library.Dialogporten.E2E.Common;

internal abstract class ServiceOwnerApiDecoratorBase : IServiceownerApi
{
    private readonly IServiceownerApi _serviceownerApi;

    internal ServiceOwnerApiDecoratorBase(IServiceownerApi serviceownerApi)
    {
        _serviceownerApi = serviceownerApi;
    }

    public virtual Task<IApiResponse> V1ServiceOwnerServiceOwnerContextQueriesGetServiceOwnerLabelServiceOwnerLabel(Guid dialogId,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerServiceOwnerContextQueriesGetServiceOwnerLabelServiceOwnerLabel(dialogId, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabelServiceOwnerLabel(Guid dialogId,
        V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabel_Label dto, Guid? if_Match,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabelServiceOwnerLabel(dialogId, dto, if_Match, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerServiceOwnerContextCommandsDeleteServiceOwnerLabelServiceOwnerLabel(Guid dialogId, string label,
        Guid? if_Match, CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerServiceOwnerContextCommandsDeleteServiceOwnerLabelServiceOwnerLabel(dialogId, label, if_Match, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(Guid dialogId, string enduserId,
        V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest setDialogSystemLabelRequest,
        Guid? if_Match, CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(dialogId, enduserId, setDialogSystemLabelRequest, if_Match, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabelsBulkSetDialogSystemLabels(string enduserId,
        V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel dto,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabelsBulkSetDialogSystemLabels(enduserId, dto, cancellationToken);

    public virtual Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchTransmissions_Transmission>>> V1ServiceOwnerDialogsQueriesSearchTransmissionsDialogTransmission(Guid dialogId,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesSearchTransmissionsDialogTransmission(dialogId, cancellationToken);

    public virtual Task<IApiResponse<string>> V1ServiceOwnerDialogsCommandsCreateTransmissionDialogTransmission(Guid dialogId,
        V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest createTransmissionRequest, Guid? if_Match,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsCreateTransmissionDialogTransmission(dialogId, createTransmissionRequest, if_Match, cancellationToken);

    public virtual Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchSeenLogs_SeenLog>>> V1ServiceOwnerDialogsQueriesSearchSeenLogsDialogSeenLog(Guid dialogId,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesSearchSeenLogsDialogSeenLog(dialogId, cancellationToken);

    public virtual Task<IApiResponse<PaginatedListOfV1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem>> V1ServiceOwnerDialogsQueriesSearchEndUserContextDialogEndUserContext(
        V1ServiceOwnerDialogsQueriesSearchEndUserContextDialogEndUserContextQueryParams queryParams,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesSearchEndUserContextDialogEndUserContext(queryParams, cancellationToken);

    public virtual Task<IApiResponse<ICollection<V1ServiceOwnerDialogsQueriesSearchActivities_Activity>>> V1ServiceOwnerDialogsQueriesSearchActivitiesDialogActivity(Guid dialogId,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesSearchActivitiesDialogActivity(dialogId, cancellationToken);

    public virtual Task<IApiResponse<string>> V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(Guid dialogId,
        V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest createActivityRequest, Guid? if_Match,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(dialogId, createActivityRequest, if_Match, cancellationToken);

    public virtual Task<IApiResponse<PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog>> V1ServiceOwnerDialogsQueriesSearchDialog(V1ServiceOwnerDialogsQueriesSearchDialogQueryParams queryParams,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(queryParams, cancellationToken);

    public virtual Task<IApiResponse<string>> V1ServiceOwnerDialogsCommandsCreateDialog(V1ServiceOwnerDialogsCommandsCreate_Dialog dto,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(dto, cancellationToken);

    public virtual Task<IApiResponse<V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationCondition>> V1ServiceOwnerDialogsQueriesNotificationConditionNotificationCondition(Guid dialogId,
        V1ServiceOwnerDialogsQueriesNotificationConditionNotificationConditionQueryParams queryParams,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesNotificationConditionNotificationCondition(dialogId, queryParams, cancellationToken);

    public virtual Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetTransmission_Transmission>> V1ServiceOwnerDialogsQueriesGetTransnissionDialogTransmission(Guid dialogId, Guid transmissionId,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesGetTransnissionDialogTransmission(dialogId, transmissionId, cancellationToken);

    public Task<IApiResponse> V1ServiceOwnerDialogsCommandsUpdateTransmissionDialogTransmission(Guid dialogId, Guid transmissionId,
        V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest updateTransmissionRequest, Guid? if_Match,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsUpdateTransmissionDialogTransmission(dialogId, transmissionId, updateTransmissionRequest, if_Match, cancellationToken);

    public virtual Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetSeenLog_SeenLog>> V1ServiceOwnerDialogsQueriesGetSeenLogDialogSeenLog(Guid dialogId, Guid seenLogId,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesGetSeenLogDialogSeenLog(dialogId, seenLogId, cancellationToken);

    public virtual Task<IApiResponse<V1ServiceOwnerDialogsQueriesGetActivity_Activity>> V1ServiceOwnerDialogsQueriesGetActivityDialogActivity(Guid dialogId, Guid activityId,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesGetActivityDialogActivity(dialogId, activityId, cancellationToken);

    public virtual Task<IApiResponse<V1ServiceOwnerDialogsQueriesGet_Dialog>> V1ServiceOwnerDialogsQueriesGetDialog(Guid dialogId, string endUserId,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(dialogId, endUserId, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerDialogsCommandsUpdateDialog(Guid dialogId, V1ServiceOwnerDialogsCommandsUpdate_Dialog dto,
        Guid? if_Match, CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsUpdateDialog(dialogId, dto, if_Match, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerDialogsCommandsDeleteDialog(Guid dialogId, Guid? if_Match,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsDeleteDialog(dialogId, if_Match, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerDialogsPatchDialog(Guid dialogId, IEnumerable<JsonPatchOperations_Operation> patchDocument, Guid? etag,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsPatchDialog(dialogId, patchDocument, etag, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerDialogsCommandsRestoreDialog(Guid dialogId, Guid? if_Match,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsRestoreDialog(dialogId, if_Match, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerDialogsCommandsPurgeDialog(Guid dialogId, Guid? if_Match,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match, cancellationToken);

    public virtual Task<IApiResponse> V1ServiceOwnerDialogsCommandsFreezeFreezeDialog(Guid dialogId, Guid? if_Match,
        CancellationToken cancellationToken = default) =>
        _serviceownerApi.V1ServiceOwnerDialogsCommandsFreezeFreezeDialog(dialogId, if_Match, cancellationToken);
}
