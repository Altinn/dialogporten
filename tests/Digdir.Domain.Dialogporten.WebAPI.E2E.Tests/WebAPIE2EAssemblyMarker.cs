using System.Reflection;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests;

public sealed class WebAPIE2EAssemblyMarker
{
    public static readonly Assembly Assembly = typeof(WebAPIE2EAssemblyMarker).Assembly;
}
