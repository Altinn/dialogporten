using Altinn.ApiClients.Dialogporten.EndUser;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using Refit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;

public static class EnduserApiExtensions
{
    extension(IEndUserApi enduserApi)
    {
        public Task<IApiResponse> SetSystemLabels(
            Guid dialogId,
            Action<V1EndUserEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest>? modify = null,
            Guid? revision = null,
            CancellationToken? cancellationToken = null)
        {
            var request = new V1EndUserEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest
            {
                AddLabels = []
            };
            modify?.Invoke(request);
            return enduserApi.V1.SetDialogSystemLabels(
                dialogId,
                request,
                revision,
                cancellationToken ?? TestContext.Current.CancellationToken);
        }

        public Task<IApiResponse<V1EndUserDialogsQueriesGet_Dialog>> GetDialog(
            Guid dialogId,
            V1EndUserCommon_AcceptedLanguages? acceptedLanguages = null,
            CancellationToken cancellationToken = default) =>
            enduserApi.V1.GetDialog(
                dialogId,
                acceptedLanguages ?? new(),
                cancellationToken);

        public Task<IApiResponse> BulkSetSystemLabels(
            Action<V1EndUserEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel> modify,
            CancellationToken? cancellationToken = null)
        {
            var request = new V1EndUserEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel();
            modify(request);
            return enduserApi.V1.BulkSetDialogSystemLabels(
                request,
                cancellationToken ?? TestContext.Current.CancellationToken);
        }

        public Task<IApiResponse<ICollection<V1EndUserEndUserContextQueriesSearchLabelAssignmentLog_LabelAssignmentLog>>> GetSystemLabelAssignmentLog(
            Guid dialogId,
            CancellationToken? cancellationToken = null) =>
            enduserApi.V1.SearchDialogLabelAssignmentLogs(
                dialogId,
                cancellationToken ?? TestContext.Current.CancellationToken);
    }
}
