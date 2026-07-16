namespace Digdir.Domain.Dialogporten.Domain.Common;

public sealed class ServiceResourceUrnFactory
{
    public static string CreateUrn(string resourceId) => $"{Constants.ServiceResourcePrefix}{resourceId}";
}
