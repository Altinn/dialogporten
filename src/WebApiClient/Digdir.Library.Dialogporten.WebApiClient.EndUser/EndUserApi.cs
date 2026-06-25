namespace Altinn.ApiClients.Dialogporten.EndUser;

/// <inheritdoc />
public sealed class EndUserApi : IEndUserApi
{
    /// <inheritdoc />
    public Features.V1.IEnduserApi V1 { get; }

    public EndUserApi(Features.V1.IEnduserApi v1)
    {
        V1 = v1;
    }
}
