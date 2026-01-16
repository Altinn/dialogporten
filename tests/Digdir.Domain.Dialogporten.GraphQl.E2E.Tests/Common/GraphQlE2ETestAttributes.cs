using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public static class GraphQlE2EExplicitOptions
{
    public const bool ExplicitTests = true;
}

public sealed class GraphQlE2EFactAttribute : FactAttribute, IBeforeAfterTestAttribute
{
    public GraphQlE2EFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Explicit = GraphQlE2EExplicitOptions.ExplicitTests;
    }

    public void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IGraphQlE2ETestHooks hooks)
        {
            hooks.BeforeTest();
        }
    }

    public void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IGraphQlE2ETestHooks hooks)
        {
            hooks.AfterTest();
        }
    }
}

public sealed class GraphQlE2ETheoryAttribute : TheoryAttribute, IBeforeAfterTestAttribute
{
    public GraphQlE2ETheoryAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Explicit = GraphQlE2EExplicitOptions.ExplicitTests;
    }

    public void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IGraphQlE2ETestHooks hooks)
        {
            hooks.BeforeTest();
        }
    }

    public void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IGraphQlE2ETestHooks hooks)
        {
            hooks.AfterTest();
        }
    }
}
