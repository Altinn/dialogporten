using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1;
using Refit;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;

public static class EnduserApiExtensions
{
    extension(IEnduserApi enduserApi)
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
            return enduserApi.V1EndUserEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
                dialogId,
                request,
                revision,
                cancellationToken ?? TestContext.Current.CancellationToken);
        }

        public Task<IApiResponse<V1EndUserDialogsQueriesGet_Dialog>> GetDialog(
            Guid dialogId,
            V1EndUserCommon_AcceptedLanguages? acceptedLanguages = null,
            CancellationToken cancellationToken = default) =>
            enduserApi.V1EndUserDialogsQueriesGetDialog(
                dialogId,
                acceptedLanguages ?? new(),
                cancellationToken);

        public Task<IApiResponse<ICollection<V1EndUserEndUserContextQueriesSearchLabelAssignmentLog_LabelAssignmentLog>>> GetSystemLabelAssignmentLog(
            Guid dialogId,
            CancellationToken? cancellationToken = null) =>
            enduserApi.V1EndUserEndUserContextQueriesSearchLabelAssignmentLogsDialogLabelAssignmentLog(
                dialogId,
                cancellationToken ?? TestContext.Current.CancellationToken);
    }
}
