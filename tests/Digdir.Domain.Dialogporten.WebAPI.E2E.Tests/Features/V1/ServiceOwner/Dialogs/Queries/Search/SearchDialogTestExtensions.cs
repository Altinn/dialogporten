using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

internal static class SearchDialogTestExtensions
{
    extension(IServiceownerApi serviceownerApi)
    {
        public Task<Guid> CreateSearchDialogAsync(
            string serviceOwnerLabel,
            Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null) =>
            serviceownerApi.CreateSimpleDialogAsync(dialog =>
            {
                dialog.ServiceOwnerContext = new()
                {
                    ServiceOwnerLabels =
                    [
                        new() { Value = serviceOwnerLabel }
                    ]
                };

                modify?.Invoke(dialog);
            });
    }
}
