using System.Globalization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests.Mapping.EndUser;

public class EndUserDialogMappingExtensionsTests
{
    [Fact]
    public void ToDialog_FromListItem_MapsScalarsContentAndSeenLog()
    {
        var source = FullListItem();

        var result = source.ToDialog();

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.Org, result.Org);
        Assert.Equal(source.ServiceResource, result.ServiceResource);
        Assert.Equal(source.ServiceResourceType, result.ServiceResourceType);
        Assert.Equal(source.Party, result.Party);
        Assert.Equal(source.Progress, result.Progress);
        Assert.Equal(source.Process, result.Process);
        Assert.Equal(source.PrecedingProcess, result.PrecedingProcess);
        Assert.Equal(source.ExtendedStatus, result.ExtendedStatus);
        Assert.Equal(source.ExternalReference, result.ExternalReference);
        Assert.Equal(source.DueAt, result.DueAt);
        Assert.Equal(source.CreatedAt, result.CreatedAt);
        Assert.Equal(source.UpdatedAt, result.UpdatedAt);
        Assert.Equal(source.ContentUpdatedAt, result.ContentUpdatedAt);
        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.IsApiOnly, result.IsApiOnly);
        Assert.Equal(source.HasUnopenedContent, result.HasUnopenedContent);
        Assert.Equal(source.IsContentSeen, result.IsContentSeen);
        Assert.Equal(source.FromServiceOwnerTransmissionsCount, result.FromServiceOwnerTransmissionsCount);
        Assert.Equal(source.FromPartyTransmissionsCount, result.FromPartyTransmissionsCount);

        // Content summary is widened; the shared Title ContentValue is reused by reference and the summary's
        // absent fields are left null.
        Assert.NotNull(result.Content);
        Assert.Same(source.Content!.Title, result.Content.Title);
        Assert.Null(result.Content.AdditionalInfo);
        Assert.Null(result.Content.MainContentReference);

        // Seen-log entries are normalized and the shared SeenBy actor is reused by reference.
        var seen = Assert.Single(result.SeenSinceLastUpdate!);
        Assert.Same(source.SeenSinceLastUpdate!.Single().SeenBy, seen.SeenBy);
        Assert.Single(result.SeenSinceLastContentUpdate!);

        // The end-user context (and its SystemLabels collection) is carried across.
        Assert.Equal(source.EndUserContext.Revision, result.EndUserContext.Revision);
        Assert.Same(source.EndUserContext.SystemLabels, result.EndUserContext.SystemLabels);
    }

    [Fact]
    public void ToDialog_FromListItem_NormalizesLatestActivityIntoSingleElementList()
    {
        var source = FullListItem();

        var result = source.ToDialog();

        var activity = Assert.Single(result.Activities!);
        Assert.Equal(source.LatestActivity!.Id, activity.Id);
        Assert.Same(source.LatestActivity!.PerformedBy, activity.PerformedBy);
    }

    [Fact]
    public void ToDialog_FromListItem_DropsCollectionHeavyAndUnavailableFields()
    {
        var source = FullListItem();

        var result = source.ToDialog();

        // DialogListItem carries none of these, so they keep their model defaults (empty collections for the
        // child collections, null/default for the scalar server fields).
        Assert.Empty(result.Transmissions!);
        Assert.Empty(result.ApiActions!);
        Assert.Empty(result.GuiActions!);
        Assert.Empty(result.Attachments!);
        Assert.Null(result.DialogToken);
        Assert.Null(result.ExpiresAt);
        Assert.Equal(Guid.Empty, result.Revision);
    }

    [Fact]
    public void ToDialog_FromListItem_WithoutLatestActivity_LeavesActivitiesNull()
    {
        var source = FullListItem();
        source.LatestActivity = null;

        var result = source.ToDialog();

        Assert.Null(result.Activities);
    }

    [Fact]
    public void ToDialog_FromListItem_WithoutContent_LeavesContentNull()
    {
        var source = FullListItem();
        source.Content = null;

        var result = source.ToDialog();

        Assert.Null(result.Content);
    }

    private static ContentValue ContentValueOf(string value) => new()
    {
        MediaType = "text/plain",
        Value = [new Localization { LanguageCode = "en", Value = value }],
    };

    private static DateTimeOffset At(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);

    private static DialogListItem FullListItem() => new()
    {
        Id = Guid.NewGuid(),
        Org = "digdir",
        ServiceResource = "urn:altinn:resource:test",
        ServiceResourceType = "GenericAccessResource",
        Party = "urn:altinn:organization:identifier-no:123456789",
        Progress = 42,
        Process = "process",
        PrecedingProcess = "preceding",
        GuiAttachmentCount = 3,
        ExtendedStatus = "extended",
        ExternalReference = "ext-ref",
        CreatedAt = At("2024-01-01T00:00:00Z"),
        UpdatedAt = At("2024-06-01T00:00:00Z"),
        ContentUpdatedAt = At("2024-06-02T00:00:00Z"),
        DueAt = At("2030-01-01T00:00:00Z"),
        Status = DialogStatus.InProgress,
        HasUnopenedContent = true,
        IsApiOnly = true,
        FromServiceOwnerTransmissionsCount = 2,
        FromPartyTransmissionsCount = 1,
        IsContentSeen = true,
        Content = new DialogContentSummary { Title = ContentValueOf("Title"), Summary = ContentValueOf("Summary") },
        LatestActivity = new DialogActivityListItem
        {
            Id = Guid.NewGuid(),
            Type = DialogActivityType.Information,
            PerformedBy = new Actor { ActorType = ActorType.ServiceOwner },
        },
        SeenSinceLastUpdate =
        [
            new DialogSeenLogListItem { Id = Guid.NewGuid(), SeenAt = At("2024-06-01T00:00:00Z"), SeenBy = new Actor { ActorType = ActorType.PartyRepresentative } },
        ],
        SeenSinceLastContentUpdate =
        [
            new DialogSeenLogListItem { Id = Guid.NewGuid(), SeenAt = At("2024-06-02T00:00:00Z"), SeenBy = new Actor { ActorType = ActorType.PartyRepresentative } },
        ],
        EndUserContext = new DialogEndUserContextListItem
        {
            Revision = Guid.NewGuid(),
            SystemLabels = [SystemLabel.Archive],
        },
    };
}
