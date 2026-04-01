using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;
using Refit;

namespace Digdir.Library.Dialogporten.E2E.Common;

public abstract class ServiceOwnerV1DecoratorBase(IServiceOwnerV1 serviceOwnerV1) : IServiceOwnerV1
{
    public virtual Task<IApiResponse<string>> CreateDialog(CreateDialogRequest request, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.CreateDialog(request, cancellationToken);

    public virtual Task<IApiResponse<Dialog>> GetDialog(Guid dialogId, string? endUserId = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.GetDialog(dialogId, endUserId, cancellationToken);

    public virtual Task<IApiResponse<PaginatedDialogList>> ListDialogs(SearchDialogQueryParams? queryParams = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.ListDialogs(queryParams, cancellationToken);

    public virtual Task<IApiResponse> UpdateDialog(Guid dialogId, UpdateDialogRequest request, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.UpdateDialog(dialogId, request, revision, cancellationToken);

    public virtual Task<IApiResponse> PatchDialog(Guid dialogId, IEnumerable<PatchOperation> patchDocument, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.PatchDialog(dialogId, patchDocument, revision, cancellationToken);

    public virtual Task<IApiResponse> DeleteDialog(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.DeleteDialog(dialogId, revision, cancellationToken);

    public virtual Task<IApiResponse> RestoreDialog(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.RestoreDialog(dialogId, revision, cancellationToken);

    public virtual Task<IApiResponse> PurgeDialog(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.PurgeDialog(dialogId, revision, cancellationToken);

    public virtual Task<IApiResponse> FreezeDialog(Guid dialogId, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.FreezeDialog(dialogId, revision, cancellationToken);

    public virtual Task<IApiResponse<string>> CreateTransmission(Guid dialogId, CreateTransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.CreateTransmission(dialogId, request, revision, cancellationToken);

    public virtual Task<IApiResponse<Transmission>> GetTransmission(Guid dialogId, Guid transmissionId, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.GetTransmission(dialogId, transmissionId, cancellationToken);

    public virtual Task<IApiResponse<ICollection<TransmissionSummary>>> ListTransmissions(Guid dialogId, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.ListTransmissions(dialogId, cancellationToken);

    public virtual Task<IApiResponse> UpdateTransmission(Guid dialogId, Guid transmissionId, UpdateTransmissionRequest request, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.UpdateTransmission(dialogId, transmissionId, request, revision, cancellationToken);

    public virtual Task<IApiResponse<string>> CreateActivity(Guid dialogId, CreateActivityRequest request, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.CreateActivity(dialogId, request, revision, cancellationToken);

    public virtual Task<IApiResponse<Activity>> GetActivity(Guid dialogId, Guid activityId, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.GetActivity(dialogId, activityId, cancellationToken);

    public virtual Task<IApiResponse<ICollection<ActivitySummary>>> ListActivities(Guid dialogId, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.ListActivities(dialogId, cancellationToken);

    public virtual Task<IApiResponse> GetServiceOwnerLabels(Guid dialogId, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.GetServiceOwnerLabels(dialogId, cancellationToken);

    public virtual Task<IApiResponse> AddServiceOwnerLabel(Guid dialogId, CreateServiceOwnerLabelRequest label, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.AddServiceOwnerLabel(dialogId, label, revision, cancellationToken);

    public virtual Task<IApiResponse> DeleteServiceOwnerLabel(Guid dialogId, string label, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.DeleteServiceOwnerLabel(dialogId, label, revision, cancellationToken);

    public virtual Task<IApiResponse> SetSystemLabel(Guid dialogId, string endUserId, SetSystemLabelRequest request, Guid? revision = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.SetSystemLabel(dialogId, endUserId, request, revision, cancellationToken);

    public virtual Task<IApiResponse> BulkSetSystemLabels(string endUserId, BulkSetSystemLabelsRequest request, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.BulkSetSystemLabels(endUserId, request, cancellationToken);

    public virtual Task<IApiResponse<SeenLog>> GetSeenLog(Guid dialogId, Guid seenLogId, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.GetSeenLog(dialogId, seenLogId, cancellationToken);

    public virtual Task<IApiResponse<ICollection<SeenLogSummary>>> ListSeenLogs(Guid dialogId, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.ListSeenLogs(dialogId, cancellationToken);

    public virtual Task<IApiResponse<PaginatedEndUserContextList>> ListEndUserContext(SearchEndUserContextQueryParams queryParams, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.ListEndUserContext(queryParams, cancellationToken);

    public virtual Task<IApiResponse<DialogLookup>> GetDialogLookup(string instanceRef, AcceptedLanguages? acceptLanguage = null, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.GetDialogLookup(instanceRef, acceptLanguage, cancellationToken);

    public virtual Task<IApiResponse<NotificationCondition>> GetNotificationCondition(Guid dialogId, NotificationConditionQueryParams queryParams, CancellationToken cancellationToken = default) =>
        serviceOwnerV1.GetNotificationCondition(dialogId, queryParams, cancellationToken);
}
