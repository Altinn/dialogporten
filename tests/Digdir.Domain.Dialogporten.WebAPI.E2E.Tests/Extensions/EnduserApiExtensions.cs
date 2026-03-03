using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1;
using Refit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;

public static class EnduserApiExtensions
{
    extension(IEnduserApi enduserApi)
    {
        public Task<IApiResponse<V1EndUserDialogsQueriesGet_Dialog>> GetDialog(
            Guid dialogId,
            V1EndUserCommon_AcceptedLanguages? acceptedLanguages = null,
            CancellationToken cancellationToken = default) =>
            enduserApi.V1EndUserDialogsQueriesGetDialog(
                dialogId,
                acceptedLanguages ?? new(),
                cancellationToken);
    }
}
