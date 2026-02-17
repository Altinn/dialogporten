using Altinn.ApiClients.Dialogporten.Features.V1;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

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

            return createDialogResponse.Content.ToGuid();
        }
    }
}
