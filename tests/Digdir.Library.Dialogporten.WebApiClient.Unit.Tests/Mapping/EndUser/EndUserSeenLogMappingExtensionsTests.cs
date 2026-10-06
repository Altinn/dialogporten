using System.Globalization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests.Mapping.EndUser;

public class EndUserSeenLogMappingExtensionsTests
{
    private static readonly DateTimeOffset SeenAt = DateTimeOffset.Parse("2024-02-01T00:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public void ToDialogSeenLog_FromDetails_MapsAllFieldsAndReusesActorByReference()
    {
        var seenBy = new Actor { ActorType = ActorType.PartyRepresentative };
        var source = new DialogSeenLogDetails
        {
            Id = Guid.NewGuid(),
            SeenAt = SeenAt,
            SeenBy = seenBy,
            IsViaServiceOwner = true,
            IsCurrentEndUser = true,
        };

        var result = source.ToDialogSeenLog();

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.SeenAt, result.SeenAt);
        Assert.Same(seenBy, result.SeenBy);
        // Non-nullable IsViaServiceOwner widens into the base nullable field.
        Assert.Equal(true, result.IsViaServiceOwner);
        Assert.True(result.IsCurrentEndUser);
    }

    [Fact]
    public void ToDialogSeenLog_FromSearchItem_MapsAllFields()
    {
        var source = new DialogSeenLogSearchItem
        {
            Id = Guid.NewGuid(),
            SeenAt = SeenAt,
            SeenBy = new Actor { ActorType = ActorType.PartyRepresentative },
            IsViaServiceOwner = false,
            IsCurrentEndUser = false,
        };

        var result = source.ToDialogSeenLog();

        Assert.Same(source.SeenBy, result.SeenBy);
        Assert.Equal(false, result.IsViaServiceOwner);
        Assert.False(result.IsCurrentEndUser);
    }

    [Fact]
    public void ToDialogSeenLog_FromListItem_PreservesNullableIsViaServiceOwner()
    {
        var source = new DialogSeenLogListItem
        {
            Id = Guid.NewGuid(),
            SeenAt = SeenAt,
            SeenBy = new Actor { ActorType = ActorType.PartyRepresentative },
            IsViaServiceOwner = null,
            IsCurrentEndUser = true,
        };

        var result = source.ToDialogSeenLog();

        Assert.Same(source.SeenBy, result.SeenBy);
        Assert.Null(result.IsViaServiceOwner);
    }
}
