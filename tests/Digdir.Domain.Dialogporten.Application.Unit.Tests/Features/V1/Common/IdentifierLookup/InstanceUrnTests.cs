using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using AwesomeAssertions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.IdentifierLookup;

public class InstanceUrnTests
{
    [Theory]
    [InlineData("urn:altinn:app-instance-id:4b6ed1db-9307-4066-8282-08391cec3d56", InstanceUrnType.AppInstanceId)]
    [InlineData("urn:altinn:correspondence-id:7c9e6679-7425-40de-944b-e07fc1f90ae7", InstanceUrnType.CorrespondenceId)]
    [InlineData("urn:altinn:dialog-id:019c1aa8-36c8-706c-9a65-675ff1fbd140", InstanceUrnType.DialogId)]
    public void TryParse_Should_Parse_Supported_Formats(string value, InstanceUrnType expectedType)
    {
        var parsed = InstanceUrn.TryParse(value, out var urn);

        parsed.Should().BeTrue();
        urn.Type.Should().Be(expectedType);
        urn.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("urn:altinn:unsupported:4b6ed1db-9307-4066-8282-08391cec3d56")]
    [InlineData("urn:altinn:app-instance-id:not-a-guid")]
    [InlineData("urn:altinn:legacy-instance-id:1337/4b6ed1db-9307-4066-8282-08391cec3d56")]
    public void TryParse_Should_Reject_Unsupported_Formats(string? value)
    {
        var parsed = InstanceUrn.TryParse(value, out _);

        parsed.Should().BeFalse();
    }
}
