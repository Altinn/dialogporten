namespace Digdir.Domain.Dialogporten.Domain.Common;

public static class ServiceResourceUrnFactory
{
    public static string CreateUrn(string resourceId) => $"{Constants.ServiceResourcePrefix}{resourceId}";
}
