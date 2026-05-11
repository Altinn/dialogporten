using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AltinnAuthorizationClientTests
{
    [Fact]
    public void InstanceRef_ToLookupLabel_Should_Map_AppInstanceRef_To_StorageLabel()
    {
        var instanceId = Guid.NewGuid();
        var rawInstanceRef = $"urn:altinn:instance-id:1337/{instanceId}";

        var parsed = InstanceRef.TryParse(rawInstanceRef, out var instanceRef);

        Assert.True(parsed);
        Assert.Equal(
            $"urn:altinn:integration:storage:1337/{instanceId}",
            instanceRef!.Value.ToLookupLabel());
    }

    [Fact]
    public void InstanceRef_ToLookupLabel_Should_Keep_CorrespondenceRef_Unchanged()
    {
        var correspondenceRef = $"urn:altinn:correspondence-id:{Guid.NewGuid()}";
        var parsed = InstanceRef.TryParse(correspondenceRef, out var instanceRef);

        Assert.True(parsed);
        Assert.Equal(correspondenceRef, instanceRef!.Value.ToLookupLabel());
    }

    [Fact]
    public void InstanceRef_Should_Parse_DialogRef()
    {
        var dialogId = Guid.NewGuid();
        var dialogRef = $"urn:altinn:dialog-id:{dialogId}";

        var parsed = InstanceRef.TryParse(dialogRef, out var instanceRef);

        Assert.True(parsed);
        Assert.Equal(InstanceRefType.DialogId, instanceRef!.Value.Type);
        Assert.Equal(dialogId, instanceRef.Value.Id);
    }
}
