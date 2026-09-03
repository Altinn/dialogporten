using System.Globalization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;
using GetModels = Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests.Mapping.EndUser;

public class EndUserActivityMappingExtensionsTests
{
    [Fact]
    public void ToDialogActivity_FromDetails_MapsAllFieldsAndReusesActorByReference()
    {
        var source = new GetModels.DialogActivityDetails
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Parse("2024-02-01T00:00:00Z", CultureInfo.InvariantCulture),
            ExtendedType = new Uri("urn:example:type"),
            Type = DialogActivityType.Information,
            TransmissionId = Guid.NewGuid(),
            PerformedBy = new Actor { ActorType = ActorType.ServiceOwner },
            Description = [new Localization { LanguageCode = "en", Value = "desc" }],
        };

        var result = source.ToDialogActivity();

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.CreatedAt, result.CreatedAt);
        Assert.Equal(source.ExtendedType, result.ExtendedType);
        Assert.Equal(source.Type, result.Type);
        Assert.Equal(source.TransmissionId, result.TransmissionId);
        Assert.Same(source.PerformedBy, result.PerformedBy);
        Assert.Same(source.Description, result.Description);
    }

    [Fact]
    public void ToDialogActivity_FromListItem_MapsAllFields()
    {
        var source = new DialogActivityListItem
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Parse("2024-02-01T00:00:00Z", CultureInfo.InvariantCulture),
            Type = DialogActivityType.Information,
            PerformedBy = new Actor { ActorType = ActorType.ServiceOwner },
        };

        var result = source.ToDialogActivity();

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.CreatedAt, result.CreatedAt);
        Assert.Same(source.PerformedBy, result.PerformedBy);
    }

    [Fact]
    public void ToDialogActivity_FromSearchItem_MapsNonNullableCreatedAtAndLeavesPerformedByNull()
    {
        var source = new DialogActivitySearchItem
        {
            Id = Guid.NewGuid(),
            // CreatedAt is non-nullable on the search item and widens into the base nullable field.
            CreatedAt = DateTimeOffset.Parse("2024-02-01T00:00:00Z", CultureInfo.InvariantCulture),
            Type = DialogActivityType.Information,
            TransmissionId = Guid.NewGuid(),
            Description = [new Localization { LanguageCode = "en", Value = "desc" }],
        };

        var result = source.ToDialogActivity();

        Assert.Equal(source.CreatedAt, result.CreatedAt);
        Assert.Equal(source.TransmissionId, result.TransmissionId);
        Assert.Same(source.Description, result.Description);
        // The search item carries no PerformedBy, so the base actor is left null.
        Assert.Null(result.PerformedBy);
    }
}
