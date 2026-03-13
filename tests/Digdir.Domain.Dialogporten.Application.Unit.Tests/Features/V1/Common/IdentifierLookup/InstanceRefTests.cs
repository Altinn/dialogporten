using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using AwesomeAssertions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.IdentifierLookup;

public class InstanceRefTests
{
    [Theory]
    [InlineData("urn:altinn:instance-id:1337/4b6ed1db-9307-4066-8282-08391cec3d56", InstanceRefType.AppInstanceId, "4b6ed1db-9307-4066-8282-08391cec3d56", 1337)]
    [InlineData("urn:altinn:correspondence-id:7c9e6679-7425-40de-944b-e07fc1f90ae7", InstanceRefType.CorrespondenceId, "7c9e6679-7425-40de-944b-e07fc1f90ae7", null)]
    [InlineData("urn:altinn:dialog-id:019c1aa8-36c8-706c-9a65-675ff1fbd140", InstanceRefType.DialogId, "019c1aa8-36c8-706c-9a65-675ff1fbd140", null)]
    public void TryParse_Should_Parse_Supported_Formats(string value, InstanceRefType expectedType, string expectedId, int? expectedPartyId)
    {
        var parsed = InstanceRef.TryParse(value, out var instanceRef);

        parsed.Should().BeTrue();
        instanceRef.Should().NotBeNull();
        instanceRef.Value.Type.Should().Be(expectedType);
        instanceRef.Value.Value.Should().Be(value);
        instanceRef.Value.Id.Should().Be(Guid.Parse(expectedId));
        instanceRef.Value.PartyId.Should().Be(expectedPartyId);
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
        var parsed = InstanceRef.TryParse(value, out var instanceRef);

        parsed.Should().BeFalse();
        instanceRef.Should().BeNull();
    }

    [Fact]
    public void FromDialog_Should_Prefer_AppInstanceRef_From_StorageLabel()
    {
        var dialogId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var result = InstanceRef.FromDialog(dialogId,
            [
                $"urn:altinn:correspondence-id:{Guid.NewGuid()}",
                $"urn:altinn:integration:storage:1337/{instanceId}"
            ]);

        result.Type.Should().Be(InstanceRefType.AppInstanceId);
        result.Id.Should().Be(instanceId);
        result.PartyId.Should().Be(1337);
        result.Value.Should().Be($"urn:altinn:instance-id:1337/{instanceId}");
    }

    [Fact]
    public void FromDialog_Should_Fallback_To_CorrespondenceRef_When_No_AppLabel()
    {
        var dialogId = Guid.NewGuid();
        var correspondenceId = Guid.NewGuid();

        var result = InstanceRef.FromDialog(dialogId,
            [
                "urn:altinn:unsupported:abc",
                $"urn:altinn:correspondence-id:{correspondenceId}"
            ]);

        result.Type.Should().Be(InstanceRefType.CorrespondenceId);
        result.Id.Should().Be(correspondenceId);
        result.Value.Should().Be($"urn:altinn:correspondence-id:{correspondenceId}");
    }

    [Fact]
    public void FromDialog_Should_Fallback_To_DialogRef_When_No_Supported_Labels()
    {
        var dialogId = Guid.NewGuid();

        var result = InstanceRef.FromDialog(dialogId, ["urn:altinn:unsupported:abc"]);

        result.Type.Should().Be(InstanceRefType.DialogId);
        result.Id.Should().Be(dialogId);
        result.Value.Should().Be($"urn:altinn:dialog-id:{dialogId}");
    }

    [Fact]
    public void FromDialog_DialogEntity_Overload_Should_Use_ServiceOwnerLabels()
    {
        var dialogId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var dialog = new DialogEntity
        {
            Id = dialogId,
            ServiceOwnerContext = new DialogServiceOwnerContext
            {
                ServiceOwnerLabels =
                [
                    new DialogServiceOwnerLabel
                    {
                        Value = $"urn:altinn:integration:storage:1337/{instanceId}"
                    }
                ]
            }
        };

        var result = InstanceRef.FromDialog(dialog);

        result.Type.Should().Be(InstanceRefType.AppInstanceId);
        result.Value.Should().Be($"urn:altinn:instance-id:1337/{instanceId}");
    }
}
