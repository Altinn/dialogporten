using System.Globalization;
using System.Reflection;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests.Mapping;

// The read model (AuthorizationContext) and the write model (AuthorizationContextInput) are distinct types, so
// a read-modify-write round trip silently drops every authorization context unless each map carries it across.
public class AuthorizationContextMappingExtensionsTests
{
    [Fact]
    public void ToUpdateDialog_FromGet_CarriesAuthorizationContextsAcrossTheWholeTree()
    {
        var source = DialogWithContexts();

        var result = source.ToUpdateDialog();

        AssertSameValues(source.Attachments.Single().AuthorizationContext, result.Attachments.Single().AuthorizationContext);
        AssertSameValues(source.GuiActions.Single().AuthorizationContext, result.GuiActions.Single().AuthorizationContext);
        AssertSameValues(source.ApiActions.Single().AuthorizationContext, result.ApiActions.Single().AuthorizationContext);

        var transmission = source.Transmissions.Single();
        var mapped = result.Transmissions.Single();
        AssertSameValues(transmission.AuthorizationContext, mapped.AuthorizationContext);
        AssertSameValues(transmission.Attachments.Single().AuthorizationContext, mapped.Attachments.Single().AuthorizationContext);
        AssertSameValues(transmission.NavigationalActions.Single().AuthorizationContext, mapped.NavigationalActions.Single().AuthorizationContext);
    }

    [Fact]
    public void ToCreateDialog_FromGet_CarriesAuthorizationContexts()
    {
        var source = DialogWithContexts();

        var result = source.ToCreateDialog();

        AssertSameValues(source.Attachments.Single().AuthorizationContext, result.Attachments.Single().AuthorizationContext);
        AssertSameValues(source.Transmissions.Single().AuthorizationContext, result.Transmissions.Single().AuthorizationContext);
    }

    [Fact]
    public void ToUpdateDialog_FromGet_CopiesPartiesRatherThanAliasingThem()
    {
        var source = DialogWithContexts();

        var result = source.ToUpdateDialog();
        result.Attachments.Single().AuthorizationContext!.Parties.Add("urn:altinn:organization:identifier-no:987654321");

        // Editing the parties of the context you are about to write must not mutate the one you read.
        Assert.Single(source.Attachments.Single().AuthorizationContext!.Parties);
    }

    [Fact]
    public void ToUpdateDialog_FromCreate_ReusesTheInputContextByReference()
    {
        var source = DialogWithContexts().ToCreateDialog();

        var result = source.ToUpdateDialog();

        // Create and Update share the one input carrier, so there is nothing to convert.
        Assert.Same(source.Attachments.Single().AuthorizationContext, result.Attachments.Single().AuthorizationContext);
    }

    [Fact]
    public void ToDialog_FromCreate_ConvertsTheInputContextBackToTheReadModel()
    {
        var source = DialogWithContexts().ToCreateDialog();

        var result = source.ToDialog();

        var expected = source.Attachments.Single().AuthorizationContext;
        var actual = result.Attachments.Single().AuthorizationContext;
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.ServiceResource, actual.ServiceResource);
        Assert.Equal(expected.AdditionalResourceAttribute, actual.AdditionalResourceAttribute);
        Assert.Equal(expected.Parties, actual.Parties);
        Assert.Equal(expected.IncludeDialogParty, actual.IncludeDialogParty);
        Assert.Equal(expected.Action, actual.Action);
        Assert.Equal(expected.UnauthorizedPresentation, actual.UnauthorizedPresentation);
    }

    [Fact]
    public void ToUpdateDialog_FromGet_LeavesAMissingContextNull()
    {
        var source = DialogWithContexts();
        source.Attachments.Single().AuthorizationContext = null;

        var result = source.ToUpdateDialog();

        Assert.Null(result.Attachments.Single().AuthorizationContext);
    }

    [Fact]
    public void ToDialog_FromCreate_MapsAnAbsentLegacyActionToTheEmptySentinel()
    {
        var source = DialogWithContexts().ToCreateDialog();
        source.GuiActions.Single().Action = null;
        source.ApiActions.Single().Action = null;

        var result = source.ToDialog();

        // Omitting the legacy action is valid on the write models when a context is supplied, but the read
        // contract's action is a non-nullable string: the server returns it empty, never absent.
        Assert.Equal(string.Empty, result.GuiActions.Single().Action);
        Assert.Equal(string.Empty, result.ApiActions.Single().Action);
    }

    [Theory]
    [InlineData(typeof(DialogGuiAction))]
    [InlineData(typeof(DialogApiAction))]
    public void GetContract_LegacyAction_StaysNonNullable(Type type)
    {
        var property = type.GetProperty(nameof(DialogGuiAction.Action))!;

        var nullability = new NullabilityInfoContext().Create(property);

        // The deprecated action is omitted from a request when a context supplies it, but a response always
        // carries it - empty for a context-governed action. Making it nullable here would misstate the
        // contract and push a spurious null check onto every consumer that still reads it.
        Assert.Equal(NullabilityState.NotNull, nullability.ReadState);
    }

    private static AuthorizationContext Context() => new()
    {
        ServiceResource = "urn:altinn:resource:other",
        AdditionalResourceAttribute = "urn:altinn:task:Task_1",
        Parties = ["urn:altinn:organization:identifier-no:123456789"],
        IncludeDialogParty = true,
        Action = "sign",
        UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Excluded,
    };

    private static ContentValue ContentValueOf(string value) => new()
    {
        MediaType = "text/plain",
        Value = [new Localization { LanguageCode = "en", Value = value }],
    };

    private static Dialog DialogWithContexts() => new()
    {
        Id = Guid.NewGuid(),
        Revision = Guid.NewGuid(),
        Org = "digdir",
        ServiceResource = "urn:altinn:resource:test",
        ServiceResourceType = "GenericAccessResource",
        Party = "urn:altinn:organization:identifier-no:123456789",
        CreatedAt = DateTimeOffset.Parse("2024-01-01T00:00:00Z", CultureInfo.InvariantCulture),
        UpdatedAt = DateTimeOffset.Parse("2024-06-01T00:00:00Z", CultureInfo.InvariantCulture),
        Status = DialogStatus.InProgress,
        Content = new Content { Title = ContentValueOf("Title"), Summary = ContentValueOf("Summary") },
        ServiceOwnerContext = new DialogServiceOwnerContext { Revision = Guid.NewGuid() },
        EndUserContext = new DialogEndUserContext { Revision = Guid.NewGuid(), SystemLabels = [SystemLabel.Default] },
        Attachments = [new DialogAttachment { Id = Guid.NewGuid(), Name = "attachment", AuthorizationContext = Context() }],
        Transmissions =
        [
            new DialogTransmission
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.Parse("2024-02-01T00:00:00Z", CultureInfo.InvariantCulture),
                Type = DialogTransmissionType.Information,
                Sender = new Actor { ActorType = ActorType.ServiceOwner },
                Content = new DialogTransmissionContent { Title = ContentValueOf("T-Title") },
                AuthorizationContext = Context(),
                Attachments = [new DialogTransmissionAttachment { Id = Guid.NewGuid(), AuthorizationContext = Context() }],
                NavigationalActions =
                [
                    new DialogTransmissionNavigationalAction { Url = new Uri("https://example.com/nav"), AuthorizationContext = Context() },
                ],
            },
        ],
        GuiActions =
        [
            new DialogGuiAction
            {
                Id = Guid.NewGuid(),
                Url = new Uri("https://example.com/gui"),
                Priority = DialogGuiActionPriority.Primary,
                HttpMethod = HttpVerb.GET,
                AuthorizationContext = Context(),
            },
        ],
        ApiActions =
        [
            new DialogApiAction
            {
                Id = Guid.NewGuid(),
                AuthorizationContext = Context(),
                Endpoints = [new DialogApiActionEndpoint { Id = Guid.NewGuid(), Url = new Uri("https://example.com/api"), HttpMethod = HttpVerb.GET }],
            },
        ],
    };

    private static void AssertSameValues(AuthorizationContext? expected, AuthorizationContextInput? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.ServiceResource, actual.ServiceResource);
        Assert.Equal(expected.AdditionalResourceAttribute, actual.AdditionalResourceAttribute);
        Assert.Equal(expected.Parties, actual.Parties);
        Assert.Equal(expected.IncludeDialogParty, actual.IncludeDialogParty);
        Assert.Equal(expected.Action, actual.Action);
        Assert.Equal(expected.UnauthorizedPresentation, actual.UnauthorizedPresentation);
    }
}
