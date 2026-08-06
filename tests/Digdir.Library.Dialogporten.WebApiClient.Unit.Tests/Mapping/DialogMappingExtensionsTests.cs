using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests.Mapping;

public class DialogMappingExtensionsTests
{
    [Fact]
    public void ToUpdateDialog_FromGet_MapsScalarsStatusContentAndChildCollections()
    {
        var source = FullDialog();

        var result = source.ToUpdateDialog();

        Assert.Equal(source.Progress, result.Progress);
        Assert.Equal(source.ExtendedStatus, result.ExtendedStatus);
        Assert.Equal(source.ExternalReference, result.ExternalReference);
        Assert.Equal(source.DueAt, result.DueAt);
        Assert.Equal(source.IsApiOnly, result.IsApiOnly);
        // Output status maps to the input enum by name.
        Assert.Equal(DialogStatusInput.InProgress, result.Status);

        Assert.NotNull(result.Content);
        // Shared ContentValue is reused by reference.
        Assert.Same(source.Content!.Title, result.Content!.Title);

        Assert.Single(result.SearchTags!);
        Assert.Single(result.Attachments!);
        Assert.Single(result.Transmissions!);
        Assert.Single(result.GuiActions!);
        Assert.Single(result.ApiActions!);
        Assert.Single(result.Activities!);

        // The deep transmission tree is carried across, and the shared Sender actor is reused by reference.
        var transmission = result.Transmissions!.Single();
        Assert.Same(source.Transmissions!.Single().Sender, transmission.Sender);
        Assert.Single(transmission.Attachments!.Single().Urls!);
    }

    [Fact]
    public void ToCreateDialog_FromGet_DefaultDropsIdentityButKeepsCreateOnlyFields()
    {
        var source = FullDialog();

        var result = source.ToCreateDialog();

        Assert.Null(result.Id);
        Assert.Null(result.IdempotentKey);
        Assert.Equal(source.ServiceResource, result.ServiceResource);
        Assert.Equal(source.Party, result.Party);
        Assert.Equal(source.VisibleFrom, result.VisibleFrom);
        Assert.Equal(source.CreatedAt, result.CreatedAt);
        Assert.Equal(DialogStatusInput.InProgress, result.Status);
        // The system label is taken from EndUserContext (the category label), not the obsolete top-level one.
        Assert.Equal(SystemLabel.Archive, result.SystemLabel);
        Assert.Single(result.ServiceOwnerContext!.ServiceOwnerLabels!);
        Assert.Equal("label", result.ServiceOwnerContext!.ServiceOwnerLabels!.Single().Value);
    }

    [Theory]
    [InlineData(SystemLabel.Bin, SystemLabel.Bin)]
    [InlineData(SystemLabel.MarkedAsUnopened, SystemLabel.Default)] // no category label -> fallback
    public void ToCreateDialog_FromGet_SystemLabelComesFromEndUserContextWithDefaultFallback(SystemLabel endUserLabel, SystemLabel expected)
    {
        var source = FullDialog();
        source.EndUserContext.SystemLabels = [endUserLabel];

        var result = source.ToCreateDialog();

        Assert.Equal(expected, result.SystemLabel);
    }

    [Fact]
    public void ToCreateDialog_FromGet_WithPreserveId_CarriesIdentity()
    {
        var source = FullDialog();

        var result = source.ToCreateDialog(preserveId: true);

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.IdempotentKey, result.IdempotentKey);
    }

    [Fact]
    public void CreateToUpdateToCreate_PreservesOverlappingFields()
    {
        var original = new CreateDialog
        {
            ServiceResource = "urn:altinn:resource:test",
            Party = "urn:altinn:organization:identifier-no:123456789",
            Progress = 42,
            ExtendedStatus = "extended",
            ExternalReference = "ext-ref",
            DueAt = DateTimeOffset.Parse("2030-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            Process = "process",
            PrecedingProcess = "preceding",
            ExpiresAt = DateTimeOffset.Parse("2031-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            IsApiOnly = true,
            Status = DialogStatusInput.Draft,
            Content = new CreateDialogContent { Title = ContentValueOf("Title") },
        };

        var roundTripped = original.ToUpdateDialog().ToCreateDialog();

        Assert.Equal(original.Progress, roundTripped.Progress);
        Assert.Equal(original.ExtendedStatus, roundTripped.ExtendedStatus);
        Assert.Equal(original.ExternalReference, roundTripped.ExternalReference);
        Assert.Equal(original.DueAt, roundTripped.DueAt);
        Assert.Equal(original.Process, roundTripped.Process);
        Assert.Equal(original.PrecedingProcess, roundTripped.PrecedingProcess);
        Assert.Equal(original.ExpiresAt, roundTripped.ExpiresAt);
        Assert.Equal(original.IsApiOnly, roundTripped.IsApiOnly);
        Assert.Equal(original.Status, roundTripped.Status);
        Assert.Same(original.Content!.Title, roundTripped.Content!.Title);
    }

    [Fact]
    public void ToUpdateDialog_FromCreate_WithoutStatus_DefaultsToNew()
    {
        var source = new CreateDialog
        {
            ServiceResource = "urn:altinn:resource:test",
            Party = "urn:altinn:organization:identifier-no:123456789",
            Status = null,
        };

        var result = source.ToUpdateDialog();

        Assert.Equal(DialogStatusInput.New, result.Status);
    }

    [Fact]
    public void ToUpdateDialog_FromGet_NullCollectionsStayNull()
    {
        var source = FullDialog();
        source.SearchTags = null;
        source.Attachments = null;
        source.Transmissions = null;

        var result = source.ToUpdateDialog();

        Assert.Null(result.SearchTags);
        Assert.Null(result.Attachments);
        Assert.Null(result.Transmissions);
    }

    [Fact]
    public void ToUpdateDialog_FromGet_EmptyCollectionsStayEmpty()
    {
        var source = FullDialog();
        source.SearchTags = [];
        source.Attachments = [];

        var result = source.ToUpdateDialog();

        Assert.Empty(result.SearchTags!);
        Assert.Empty(result.Attachments!);
    }

    private static ContentValue ContentValueOf(string value) => new()
    {
        MediaType = "text/plain",
        Value = [new Localization { LanguageCode = "en", Value = value }],
    };

    private static Dialog FullDialog() => new()
    {
        Id = Guid.NewGuid(),
        IdempotentKey = "idempotent-key",
        Revision = Guid.NewGuid(),
        Org = "digdir",
        ServiceResource = "urn:altinn:resource:test",
        ServiceResourceType = "GenericAccessResource",
        Party = "urn:altinn:organization:identifier-no:123456789",
        Progress = 42,
        ExtendedStatus = "extended",
        ExternalReference = "ext-ref",
        VisibleFrom = DateTimeOffset.Parse("2025-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
        DueAt = DateTimeOffset.Parse("2030-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
        ExpiresAt = DateTimeOffset.Parse("2031-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
        CreatedAt = DateTimeOffset.Parse("2024-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
        UpdatedAt = DateTimeOffset.Parse("2024-06-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
        IsApiOnly = true,
        Status = DialogStatus.InProgress,
        Content = new Content { Title = ContentValueOf("Title"), Summary = ContentValueOf("Summary") },
        SearchTags = [new DialogTag { Value = "tag" }],
        Attachments =
        [
            new DialogAttachment
            {
                Id = Guid.NewGuid(),
                Name = "attachment",
                Urls = [new DialogAttachmentUrl { Id = Guid.NewGuid(), Url = new Uri("https://example.com/a"), ConsumerType = AttachmentUrlConsumerType.Gui }],
            },
        ],
        Transmissions =
        [
            new DialogTransmission
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.Parse("2024-02-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                Type = DialogTransmissionType.Information,
                Sender = new Actor { ActorType = ActorType.ServiceOwner },
                Content = new DialogTransmissionContent { Title = ContentValueOf("T-Title") },
                Attachments =
                [
                    new DialogTransmissionAttachment
                    {
                        Id = Guid.NewGuid(),
                        Urls = [new DialogTransmissionAttachmentUrl { Id = Guid.NewGuid(), Url = new Uri("https://example.com/t"), ConsumerType = AttachmentUrlConsumerType.Api }],
                    },
                ],
                NavigationalActions = [new DialogTransmissionNavigationalAction { Url = new Uri("https://example.com/nav") }],
            },
        ],
        GuiActions =
        [
            new DialogGuiAction
            {
                Id = Guid.NewGuid(),
                Action = "read",
                Url = new Uri("https://example.com/gui"),
                Priority = DialogGuiActionPriority.Primary,
                HttpMethod = HttpVerb.GET,
            },
        ],
        ApiActions =
        [
            new DialogApiAction
            {
                Id = Guid.NewGuid(),
                Action = "read",
                Endpoints = [new DialogApiActionEndpoint { Id = Guid.NewGuid(), Url = new Uri("https://example.com/api"), HttpMethod = HttpVerb.GET }],
            },
        ],
        Activities =
        [
            new DialogActivity
            {
                Id = Guid.NewGuid(),
                Type = DialogActivityType.Information,
                PerformedBy = new Actor { ActorType = ActorType.ServiceOwner },
            },
        ],
        ServiceOwnerContext = new DialogServiceOwnerContext
        {
            Revision = Guid.NewGuid(),
            ServiceOwnerLabels = [new DialogServiceOwnerLabel { Value = "label" }],
        },
        EndUserContext = new DialogEndUserContext
        {
            Revision = Guid.NewGuid(),
            SystemLabels = [SystemLabel.MarkedAsUnopened, SystemLabel.Archive],
        },
    };
}
