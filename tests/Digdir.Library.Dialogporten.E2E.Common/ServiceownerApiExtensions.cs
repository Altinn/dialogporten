using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class ServiceownerApiExtensions
{
    extension(IServiceownerApi serviceownerApi)
    {
        public async Task<Guid> CreateSimpleDialogAsync(Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null)
        {
            var createDialogResponse =
                await serviceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
                    DialogTestData.CreateSimpleDialog(modify),
                    TestContext.Current.CancellationToken);

            createDialogResponse.Content.Should().NotBeNull();

            var dialogIdRaw = createDialogResponse
                .Content
                .Trim('"');

            if (!Guid.TryParse(dialogIdRaw, out var dialogId))
            {
                Assert.Fail($"Could not parse create dialog response, {dialogIdRaw}");
            }

            dialogId.Should().NotBe(Guid.Empty);

            return dialogId;
        }
    }
}
