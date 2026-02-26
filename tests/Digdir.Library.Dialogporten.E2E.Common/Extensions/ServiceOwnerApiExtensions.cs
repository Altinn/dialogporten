using Altinn.ApiClients.Dialogporten.Features.V1;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ServiceOwnerApiExtensions
{
    extension(IServiceownerApi serviceownerApi)
    {
        public async Task<Guid> CreateSimpleDialogAsync(Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null)
        {
            var createDialogResponse =
                await serviceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
                    DialogTestData.CreateSimpleDialog(modify),
                    TestContext.Current.CancellationToken);

            return createDialogResponse.Content.ToGuid();
        }

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

            return createActivityResponse.Content.ToGuid();
        }
    }
}
