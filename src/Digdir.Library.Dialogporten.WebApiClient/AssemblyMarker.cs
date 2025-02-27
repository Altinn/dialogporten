using System.Reflection;

namespace Altinn.ApiClients.Dialogporten;

internal sealed class AssemblyMarker
{
    public static readonly Assembly Assembly = typeof(AssemblyMarker).Assembly;
}
