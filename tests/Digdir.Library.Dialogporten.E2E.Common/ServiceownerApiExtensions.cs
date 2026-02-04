using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class ServiceownerApiExtensions
{
    public static async Task<Guid> CreateSimpleDialogAsync(
        this IServiceownerApi serviceownerApi,
        V1ServiceOwnerDialogsCommandsCreate_Dialog? createDialogCommand = null)
    {
        ArgumentNullException.ThrowIfNull(serviceownerApi);

        var createDialogResponse =
            await serviceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
                createDialogCommand ?? DialogTestData.CreateSimpleDialog(),
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

    public static Task<Guid> CreateSimpleDialogAsync(
        this IServiceownerApi serviceownerApi,
        Action<V1ServiceOwnerDialogsCommandsCreate_Dialog> modify) =>
        serviceownerApi.CreateSimpleDialogAsync(
            DialogTestData.CreateSimpleDialog(modify));
}
