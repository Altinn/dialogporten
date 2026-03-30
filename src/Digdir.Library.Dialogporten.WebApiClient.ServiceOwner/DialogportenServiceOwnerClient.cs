namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <inheritdoc />
public sealed class DialogportenServiceOwnerClient : IDialogportenServiceOwnerClient
{
    /// <inheritdoc />
    public IDialogportenServiceOwnerV1 V1 { get; }

    public DialogportenServiceOwnerClient(IDialogportenServiceOwnerV1 v1)
    {
        V1 = v1;
    }
}
