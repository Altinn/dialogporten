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
            Action<SetDialogSystemLabelRequest>? modify = null,
            Guid? revision = null,
            CancellationToken? cancellationToken = null)
        {
            var request = new SetDialogSystemLabelRequest
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

        public Task<IApiResponse<Dialog>> GetDialog(
            Guid dialogId,
            AcceptedLanguages? acceptedLanguages = null,
            CancellationToken cancellationToken = default) =>
            enduserApi.V1.GetDialog(
                dialogId,
                acceptedLanguages ?? new(),
                cancellationToken);

        public Task<IApiResponse> BulkSetSystemLabels(
            Action<BulkSetSystemLabel> modify,
            CancellationToken? cancellationToken = null)
        {
            var request = new BulkSetSystemLabel();
            modify(request);
            return enduserApi.V1.BulkSetDialogSystemLabels(
                request,
                cancellationToken ?? TestContext.Current.CancellationToken);
        }

        public Task<IApiResponse<ICollection<LabelAssignmentLog>>> GetSystemLabelAssignmentLog(
            Guid dialogId,
            CancellationToken? cancellationToken = null) =>
            enduserApi.V1.SearchDialogLabelAssignmentLogs(
                dialogId,
                cancellationToken ?? TestContext.Current.CancellationToken);
    }
}
