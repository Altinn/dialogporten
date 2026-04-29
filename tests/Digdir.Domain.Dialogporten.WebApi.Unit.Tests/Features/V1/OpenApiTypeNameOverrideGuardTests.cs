using System.Reflection;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.Features.V1;

public class OpenApiTypeNameOverrideGuardTests
{
    [Fact]
    public void Generate_ReturnsMappedName_ForKnownSdkContractType()
    {
        var result = Generate("v1.enduser", typeof(AcceptedLanguages));

        result.Should().Be("AcceptedLanguages");
    }

    [Fact]
    public void Generate_Throws_WhenSdkContractTypeFallsBackToNamespaceHeavyNameWithoutOverride()
    {
        static void Act()
        {
            _ = Generate("v1.enduser", typeof(UnmappedSdkContract));
        }

        var exception = Assert.Throws<TargetInvocationException>(Act);
        var innerException = Assert.IsType<InvalidOperationException>(exception.InnerException);
        innerException.Message.Should().Contain(nameof(UnmappedSdkContract));
        innerException.Message.Should().Contain("v1.enduser");
    }

    private static string Generate(string documentName, Type type)
    {
        var generatorType = typeof(Common.OpenApiTypeNameAttribute)
            .Assembly
            .GetType("Digdir.Domain.Dialogporten.WebApi.Common.Json.ShortNameGenerator")
            ?? throw new InvalidOperationException("Could not find ShortNameGenerator.");

        var generator = Activator.CreateInstance(generatorType, documentName)
            ?? throw new InvalidOperationException("Could not create ShortNameGenerator.");

        var generateMethod = generatorType.GetMethod("Generate")
            ?? throw new InvalidOperationException("Could not find ShortNameGenerator.Generate.");

        return (string)generateMethod.Invoke(generator, [type])!;
    }

    private sealed class UnmappedSdkContract;
}
