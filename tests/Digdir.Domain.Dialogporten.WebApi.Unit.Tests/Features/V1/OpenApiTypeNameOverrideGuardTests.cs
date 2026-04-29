using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Json;

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
        var exception = Assert.Throws<InvalidOperationException>(Act);
        exception.Message.Should().Contain(nameof(UnmappedSdkContract));
        exception.Message.Should().Contain("v1.enduser");
        return;

        static void Act()
        {
            _ = Generate("v1.enduser", typeof(UnmappedSdkContract));
        }
    }

    private static string Generate(string documentName, Type type) =>
        new ShortNameGenerator(documentName).Generate(type);

    private sealed class UnmappedSdkContract;
}
