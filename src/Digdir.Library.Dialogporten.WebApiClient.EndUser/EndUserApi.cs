using Altinn.ApiClients.Dialogporten.EndUser.V1;

namespace Altinn.ApiClients.Dialogporten.EndUser;

/// <inheritdoc />
public sealed class EndUserApi : IEndUserApi
{
    /// <inheritdoc />
    public IEndUserV1 V1 { get; }

    public EndUserApi(IEndUserV1 v1)
    {
        V1 = v1;
    }
}
