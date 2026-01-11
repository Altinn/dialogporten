using System.Reflection;
using Digdir.Domain.Dialogporten.GraphQl.E2E.Tests;
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
                    .Any(a => a is FactAttribute { Explicit: false } or
                            TheoryAttribute { Explicit: false }))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .OrderBy(name => name)
            .ToArray();

        nonExplicitTests
            .Should()
            .BeEmpty(
                "All [Fact] and [Theory] attributes in the GraphQL E2E test project must be marked as Explicit = true.");
    }
}
