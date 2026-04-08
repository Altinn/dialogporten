using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using Refit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ServiceOwnerApiExtensions
{
    extension(IServiceownerApi serviceownerApi)
    {
        public Task<Guid> CreateSimpleDialogAsync(Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null) =>
            serviceownerApi.CreateDialogAsync(DialogTestData.CreateSimpleDialog(modify));

        public Task<Guid> CreateComplexDialogAsync(Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null) =>
            serviceownerApi.CreateDialogAsync(DialogTestData.CreateComplexDialog(modify));

        public async Task<Guid> CreateDialogAsync(V1ServiceOwnerDialogsCommandsCreate_Dialog dialog)
        {
            var createDialogResponse =
                await serviceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
                    dialog,
                    TestContext.Current.CancellationToken);

            createDialogResponse.ShouldHaveStatusCode(HttpStatusCode.Created);

            return createDialogResponse.Content.ToGuid();
        }

        public Task<IApiResponse<V1ServiceOwnerDialogsQueriesGet_Dialog>> GetDialog(
            Guid dialogId,
            string? endUserId = null,
            CancellationToken? cancellationToken = null) =>
            serviceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
                dialogId,
                endUserId ?? null!,
                cancellationToken ?? TestContext.Current.CancellationToken);

        public async Task<Guid> CreateSimpleActivityAsync(
            Guid dialogId,
            Guid? ifMatch = null,
            Action<V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest>? modify = null)
        {
            var createActivityResponse =
                await serviceownerApi.V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(
                    dialogId,
                    DialogTestData.CreateSimpleActivity(modify),
                    ifMatch,
                    TestContext.Current.CancellationToken);

            createActivityResponse.ShouldHaveStatusCode(HttpStatusCode.Created);

            return createActivityResponse.Content.ToGuid();
        }

        public Task<IApiResponse> SetSystemLabel(
            Guid dialogId,
            string endUserId,
            Action<V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest>? modify = null,
            Guid? ifMatch = null,
            CancellationToken? cancellationToken = null)
        {
            var request = new V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest
            {
                AddLabels = [],
                RemoveLabels = []
            };

            modify?.Invoke(request);
            return serviceownerApi.V1ServiceOwnerEndUserContextCommandsSetSystemLabelSetDialogSystemLabels(
                dialogId,
                endUserId,
                request,
                ifMatch,
                cancellationToken ?? TestContext.Current.CancellationToken);
        }

        public async Task<IApiResponse> PatchDialogAsync(
            Guid dialogId,
            Action<List<JsonPatchOperations_Operation>> modify,
            Guid? ifMatch = null)
        {
            List<JsonPatchOperations_Operation> patchDocument = [];
            modify(patchDocument);
            return await serviceownerApi.V1ServiceOwnerDialogsPatchDialog(
                dialogId,
                patchDocument,
                ifMatch,
                TestContext.Current.CancellationToken);
        }
    }
}
