using System.Reflection;
using System.Runtime.CompilerServices;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit.v3;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class E2EExplicitOptions
{
    // When true, tests are marked Explicit and are skipped unless xUnit.Explicit=on/only.
    // Set to false in appsettings.local.json to run E2E tests in your IDE.
    public static bool ExplicitTests { get; } = LoadExplicitTests();

    private static bool LoadExplicitTests() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build()
            .GetValue("ExplicitTests", defaultValue: true);
}

public sealed class E2EFactAttribute : FactAttribute, IBeforeAfterTestAttribute
{
    public string[] SkipOnEnvironments { get; init; } = [];

    public E2EFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Explicit = E2EExplicitOptions.ExplicitTests;
    }

    public void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IE2ETestHooks hooks)
        {
            hooks.BeforeTest();
        }

        var env = Environment.GetDotnetEnvironment();
        if (SkipOnEnvironments.Contains(env, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{DynamicSkipToken.Value}Skipped in {env} environment.");
    }

    public void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IE2ETestHooks hooks)
        {
            hooks.AfterTest();
        }
    }
}

public sealed class E2ETheoryAttribute : TheoryAttribute, IBeforeAfterTestAttribute
{
    public string[] SkipOnEnvironments { get; init; } = [];

    public E2ETheoryAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Explicit = E2EExplicitOptions.ExplicitTests;
    }

    public void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IE2ETestHooks hooks)
        {
            hooks.BeforeTest();
        }

        var env = Environment.GetDotnetEnvironment();
        if (SkipOnEnvironments.Contains(env, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{DynamicSkipToken.Value}Skipped in {env} environment.");
    }

    public void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IE2ETestHooks hooks)
        {
            hooks.AfterTest();
        }
    }
}
