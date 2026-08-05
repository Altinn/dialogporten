using System.Reflection;

namespace Altinn.ApiClients.Dialogporten.EndUser;

internal sealed class AssemblyMarker
{
    public static readonly Assembly Assembly = typeof(AssemblyMarker).Assembly;
}
