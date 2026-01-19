using System.Reflection;
using Digdir.Domain.Dialogporten.GraphQl.E2E.Tests;
using Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Architecture.Tests;

public class GraphQlE2EFactAttributeUsageTest
{
    [Fact]
    public void GraphQl_E2E_ExplicitOption_Must_Be_Enabled() =>
        GraphQlE2EExplicitOptions.ExplicitTests
            .Should()
            .BeTrue("GraphQl E2E tests must remain explicit in CI/CD.");

    [Fact]
    public void All_GraphQL_E2E_Tests_Must_Inherit_E2E_Base()
    {
        var testMethods = GraphQlE2EAssemblyMarker
            .Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttributes(inherit: true)
                .Any(a => a is GraphQlE2EFactAttribute or GraphQlE2ETheoryAttribute))
            .ToArray();

        var nonBaseClasses = testMethods
            .Select(m => m.DeclaringType)
            .Where(t => t is not null && !t.IsSubclassOf(typeof(GraphQlE2ETestBase)))
            .Distinct()
            .Select(t => t!.FullName!)
            .OrderBy(name => name)
            .ToArray();

        nonBaseClasses
            .Should()
            .BeEmpty(
                $"All GraphQl E2E test classes must inherit {nameof(GraphQlE2ETestBase)}.");
    }

    [Fact]
    public void All_GraphQL_E2E_Tests_Must_Use_Custom_Attributes()
    {
        var nonCustomAttributeTests = GraphQlE2EAssemblyMarker
            .Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttributes(inherit: true)
                .Any(a => a is FactAttribute or TheoryAttribute))
            .Where(m => m.GetCustomAttributes(inherit: true)
                .All(a => a is not GraphQlE2EFactAttribute and not GraphQlE2ETheoryAttribute))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .OrderBy(name => name)
            .ToArray();

        nonCustomAttributeTests
            .Should()
            .BeEmpty(
                $"All tests in the GraphQl E2E project must use {nameof(GraphQlE2EFactAttribute)} " +
                $"or {nameof(GraphQlE2ETheoryAttribute)}.");
    }
}
