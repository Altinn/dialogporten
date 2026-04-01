using System.Net;
using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ServiceOwnerV1Extensions
{
    extension(IServiceOwnerV1 serviceOwnerV1)
    {
        public Task<Guid> CreateSimpleDialogAsync(Action<CreateDialogRequest>? modify = null) =>
            serviceOwnerV1.CreateDialogAsync(ServiceOwnerSdkDialogTestData.CreateSimpleDialog(modify));

        public Task<Guid> CreateComplexDialogAsync(Action<CreateDialogRequest>? modify = null) =>
            serviceOwnerV1.CreateDialogAsync(ServiceOwnerSdkDialogTestData.CreateComplexDialog(modify));

        private async Task<Guid> CreateDialogAsync(CreateDialogRequest dialog)
        {
            ArgumentNullException.ThrowIfNull(serviceOwnerV1);
            ArgumentNullException.ThrowIfNull(dialog);

            var createDialogResponse = await serviceOwnerV1.CreateDialog(dialog, TestContext.Current.CancellationToken);

            createDialogResponse.ShouldHaveStatusCode(HttpStatusCode.Created);

            return createDialogResponse.Content.ToGuid();
        }
    }
}
