using System.Globalization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests.Mapping.EndUser;

public class EndUserTransmissionMappingExtensionsTests
{
    [Fact]
    public void ToDialogTransmission_FromDetails_MapsScalarsAndDeepTree()
    {
        var source = FullDetails();

        var result = source.ToDialogTransmission();

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.CreatedAt, result.CreatedAt);
        Assert.Equal(source.AuthorizationAttribute, result.AuthorizationAttribute);
        Assert.Equal(source.IsAuthorized, result.IsAuthorized);
        Assert.Equal(source.ExtendedType, result.ExtendedType);
        Assert.Equal(source.ExternalReference, result.ExternalReference);
        Assert.Equal(source.RelatedTransmissionId, result.RelatedTransmissionId);
        Assert.Equal(source.Type, result.Type);
        // IsOpened has no source on Details and defaults to false.
        Assert.False(result.IsOpened);

        // Shared Sender actor and ContentValue title are reused by reference.
        Assert.Same(source.Sender, result.Sender);
        Assert.Same(source.Content.Title, result.Content.Title);

        var attachment = Assert.Single(result.Attachments);
        var url = Assert.Single(attachment.Urls);
        Assert.Equal(source.Attachments.Single().Urls.Single().Url, url.Url);
        Assert.Single(result.NavigationalActions);
    }

    [Fact]
    public void ToDialogTransmission_FromSearchItem_MapsScalarsAndDeepTree()
    {
        var source = FullSearchItem();

        var result = source.ToDialogTransmission();

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.IsAuthorized, result.IsAuthorized);
        Assert.Equal(source.Type, result.Type);
        Assert.False(result.IsOpened);
        Assert.Same(source.Sender, result.Sender);
        Assert.Same(source.Content.Title, result.Content.Title);
        Assert.Single(result.Attachments.Single().Urls);
        Assert.Single(result.NavigationalActions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToDialogTransmission_CarriesContextTokensAndAuthorizationFlags(bool fromDetails)
    {
        // A context token is what the caller has to present against the URLs of the entity it belongs to, so
        // losing one while normalizing the endpoint families would silently downgrade the caller to the dialog token.
        var result = fromDetails ? FullDetails().ToDialogTransmission() : FullSearchItem().ToDialogTransmission();

        Assert.Equal("transmission-context-token", result.ContextToken);

        var attachment = Assert.Single(result.Attachments);
        Assert.True(attachment.IsAuthorized);
        Assert.Equal("attachment-context-token", attachment.ContextToken);

        var navigationalAction = Assert.Single(result.NavigationalActions);
        Assert.True(navigationalAction.IsAuthorized);
        Assert.Equal("nav-context-token", navigationalAction.ContextToken);
    }

    [Fact]
    public void ToDialogTransmission_FromDetails_NullCollectionsBecomeEmpty()
    {
        var source = FullDetails();
        source.Attachments = null!;
        source.NavigationalActions = null!;

        var result = source.ToDialogTransmission();

        // The target collections are non-nullable and default to empty, so a null source
        // is normalized to an empty collection rather than propagating null.
        Assert.Empty(result.Attachments);
        Assert.Empty(result.NavigationalActions);
    }

    [Fact]
    public void ToDialogTransmission_FromSearchItem_EmptyCollectionsStayEmpty()
    {
        var source = FullSearchItem();
        source.Attachments = [];
        source.NavigationalActions = [];

        var result = source.ToDialogTransmission();

        Assert.Empty(result.Attachments);
        Assert.Empty(result.NavigationalActions);
    }

    private static ContentValue ContentValueOf(string value) => new()
    {
        MediaType = "text/plain",
        Value = [new Localization { LanguageCode = "en", Value = value }],
    };

    private static DateTimeOffset At(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);

    private static DialogTransmissionDetails FullDetails() => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = At("2024-02-01T00:00:00Z"),
        AuthorizationAttribute = "attr",
        IsAuthorized = true,
        ContextToken = "transmission-context-token",
        ExtendedType = new Uri("urn:example:type"),
        ExternalReference = "ext",
        RelatedTransmissionId = Guid.NewGuid(),
        DeletedAt = At("2024-03-01T00:00:00Z"),
        Type = DialogTransmissionType.Information,
        Sender = new Actor { ActorType = ActorType.ServiceOwner },
        Content = new DialogTransmissionContentDetails { Title = ContentValueOf("T-Title"), Summary = ContentValueOf("Sum") },
        Attachments =
        [
            new DialogTransmissionAttachmentDetails
            {
                Id = Guid.NewGuid(),
                Name = "a",
                IsAuthorized = true,
                ContextToken = "attachment-context-token",
                Urls = [new DialogTransmissionAttachmentUrlDetails { Id = Guid.NewGuid(), Url = new Uri("https://example.com/t"), ConsumerType = AttachmentUrlConsumerType.Api }],
            },
        ],
        NavigationalActions =
        [
            new DialogTransmissionNavigationalActionDetails
            {
                Url = new Uri("https://example.com/nav"),
                IsAuthorized = true,
                ContextToken = "nav-context-token",
            },
        ],
    };

    private static DialogTransmissionSearchItem FullSearchItem() => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = At("2024-02-01T00:00:00Z"),
        AuthorizationAttribute = "attr",
        IsAuthorized = true,
        ContextToken = "transmission-context-token",
        ExtendedType = new Uri("urn:example:type"),
        ExternalReference = "ext",
        RelatedTransmissionId = Guid.NewGuid(),
        DeletedAt = At("2024-03-01T00:00:00Z"),
        Type = DialogTransmissionType.Information,
        Sender = new Actor { ActorType = ActorType.ServiceOwner },
        Content = new DialogTransmissionSearchContent { Title = ContentValueOf("T-Title") },
        Attachments =
        [
            new DialogTransmissionSearchAttachment
            {
                Id = Guid.NewGuid(),
                Name = "a",
                IsAuthorized = true,
                ContextToken = "attachment-context-token",
                Urls = [new DialogTransmissionSearchAttachmentUrl { Id = Guid.NewGuid(), Url = new Uri("https://example.com/t"), ConsumerType = AttachmentUrlConsumerType.Api }],
            },
        ],
        NavigationalActions =
        [
            new DialogTransmissionSearchNavigationalAction
            {
                Url = new Uri("https://example.com/nav"),
                IsAuthorized = true,
                ContextToken = "nav-context-token",
            },
        ],
    };
}
