using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;

namespace Altinn.ApiClients.Dialogporten.EndUser;

/// <inheritdoc />
public sealed class EndUserApi : IEndUserApi
{
    /// <inheritdoc />
    public IEnduserApi V1 { get; }

    public EndUserApi(IEnduserApi v1)
    {
        V1 = v1;
    }
}
