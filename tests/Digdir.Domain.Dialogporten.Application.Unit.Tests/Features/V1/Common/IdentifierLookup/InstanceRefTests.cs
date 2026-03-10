using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using AwesomeAssertions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.IdentifierLookup;

public class InstanceRefTests
{
    [Theory]
    [InlineData("urn:altinn:instance-id:1337/4b6ed1db-9307-4066-8282-08391cec3d56", InstanceRefType.AppInstanceId)]
    [InlineData("urn:altinn:correspondence-id:7c9e6679-7425-40de-944b-e07fc1f90ae7", InstanceRefType.CorrespondenceId)]
    [InlineData("urn:altinn:dialog-id:019c1aa8-36c8-706c-9a65-675ff1fbd140", InstanceRefType.DialogId)]
    public void TryParse_Should_Parse_Supported_Formats(string value, InstanceRefType expectedType)
    {
        var parsed = InstanceRef.TryParse(value, out var instanceRef);

        parsed.Should().BeTrue();
        instanceRef.Should().NotBeNull();
        instanceRef.Value.Type.Should().Be(expectedType);
        instanceRef.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("urn:altinn:unsupported:4b6ed1db-9307-4066-8282-08391cec3d56")]
    [InlineData("urn:altinn:instance-id:1337/not-a-guid")]
    [InlineData("urn:altinn:instance-id:4b6ed1db-9307-4066-8282-08391cec3d56")]
    [InlineData("urn:altinn:legacy-instance-id:1337/4b6ed1db-9307-4066-8282-08391cec3d56")]
    public void TryParse_Should_Reject_Unsupported_Formats(string? value)
    {
        var parsed = InstanceRef.TryParse(value, out _);

        parsed.Should().BeFalse();
    }
}
