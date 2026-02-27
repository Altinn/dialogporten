using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ServiceOwnerApiExtensions
{
    extension(IServiceownerApi serviceownerApi)
    {
        public Task<Guid> CreateSimpleDialogAsync(Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null)
        {
            return serviceownerApi.CreateDialogAsync(DialogTestData.CreateSimpleDialog(modify));
        }

        public Task<Guid> CreateComplexDialogAsync(Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null)
        {
            return serviceownerApi.CreateDialogAsync(DialogTestData.CreateComplexDialog(modify));
        }

        public async Task<Guid> CreateDialogAsync(V1ServiceOwnerDialogsCommandsCreate_Dialog dialog)
        {
            var createDialogResponse =
                await serviceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
                    dialog,
                    TestContext.Current.CancellationToken);

            if (createDialogResponse.IsSuccessStatusCode) return createDialogResponse.Content.ToGuid();

            TestContext.Current.TestOutputHelper!.WriteLine(createDialogResponse.Error.Content!);
            createDialogResponse.Error.Should().BeNull();
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
