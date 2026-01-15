using System.Reflection;
using Digdir.Domain.Dialogporten.GraphQl.E2E.Tests;
using Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Architecture.Tests;

public class GraphQlE2EFactAttributeUsageTest
{
    [Fact]
    public void All_GraphQL_E2E_Tests_Must_Be_Explicit()
    {
        var nonExplicitTests = GraphQlE2EAssemblyMarker
            .Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            .Where(m =>
                m.GetCustomAttributes(inherit: true)
                    .Any(a => a is GraphQlE2EFactAttribute { Explicit: false } or
                        GraphQlE2ETheoryAttribute { Explicit: false }))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .OrderBy(name => name)
            .ToArray();

        nonExplicitTests
            .Should()
            .BeEmpty(
                "All tests in the GraphQl E2E project needs to use the attribute " +
                $"{nameof(GraphQlE2EFactAttribute)} or {nameof(GraphQlE2ETheoryAttribute)}");
    }
}
