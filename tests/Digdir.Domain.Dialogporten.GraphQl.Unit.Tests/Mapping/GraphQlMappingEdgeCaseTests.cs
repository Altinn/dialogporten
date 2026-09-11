using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using ActorType = Digdir.Domain.Dialogporten.Domain.Actors.ActorType.Values;
using DialogStatus = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus.Values;
using GuiActionPriority = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiActionPriority.Values;
using HttpVerb = Digdir.Domain.Dialogporten.Domain.Http.HttpVerb.Values;
using TransmissionType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmissionType.Values;
using Get = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Search = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Model = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Common = Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using SearchModel = Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using Mutation = Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;
using Lookup = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup;
using Party = Digdir.Domain.Dialogporten.GraphQL.EndUser.Parties;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.Mapping;

// Baselines were captured against the AutoMapper mappings and are now produced by the hand-written static
// mappers (issue #967); the verified files staying identical proves the migration is behaviour-preserving.
// Empty collections are included explicitly so Verify cannot hide null-to-empty regressions.
public sealed class GraphQlMappingEdgeCaseTests
{
    [Fact]
    public Task Search_Omitted_Filters()
    {
        var result = SearchModel.SearchDialogsMapExtensions.ToSearchDialogQuery(new SearchModel.SearchDialogInput());
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Search_Empty_Filters()
    {
        var source = new SearchModel.SearchDialogInput
        {
            Org = [],
            ServiceResource = [],
            Party = [],
            ExtendedStatus = [],
            Status = [],
            SystemLabel = [],
            OrderBy = []
        };
        var result = SearchModel.SearchDialogsMapExtensions.ToSearchDialogQuery(source);
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public Task Search_Nullable_Boolean_Filters(bool? value)
    {
        var source = new SearchModel.SearchDialogInput { ExcludeApiOnly = value, IsContentSeen = value };
        var result = SearchModel.SearchDialogsMapExtensions.ToSearchDialogQuery(source);
        return Verify(result).UseParameters(value).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Search_Empty_Last_Page()
    {
        var source = new PaginatedList<Search.DialogDto>([], false, null, "contentUpdatedAt_desc");
        var result = SearchModel.SearchDialogsMapExtensions.ToSearchDialogsPayload(source);
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Search_Null_Content_And_LatestActivity()
    {
        var source = ObjectFiller.Fill<Search.DialogDto>();
        source.Content = null;
        source.LatestActivity = null;
        var result = SearchModel.SearchDialogsMapExtensions.ToSearchDialogsPayload(
            new PaginatedList<Search.DialogDto>([source], false, null, "contentUpdatedAt_desc"));
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Dialog_Null_Optional_Content_And_Null_Or_Empty_Collections(bool empty)
    {
        var source = ObjectFiller.Fill<Get.DialogDto>();
        source.Content.Summary = null;
        source.Content.SenderName = null;
        source.Content.AdditionalInfo = null;
        source.Content.ExtendedStatus = null;
        source.Content.MainContentReference = null;
        source.DueAt = null;
        source.ExpiresAt = null;
        source.Progress = null;
        source.DialogToken = null;
        source.ExcludedAttachments = empty ? [] : null;
        source.ExcludedTransmissions = empty ? [] : null;
        source.ExcludedGuiActions = empty ? [] : null;
        source.ExcludedApiActions = empty ? [] : null;
        source.Attachments = [];
        source.ApiActions = [];
        source.Activities = [];
        source.SeenSinceLastUpdate = [];
        source.SeenSinceLastContentUpdate = [];
        source.GuiActions = [new() { Priority = GuiActionPriority.Primary, HttpMethod = HttpVerb.GET, Prompt = empty ? [] : null }];
        source.Transmissions = [new()
        {
            Content = new() { Title = new() },
            Sender = new() { ActorType = ActorType.ServiceOwner },
            Type = TransmissionType.Information,
            ExcludedAttachments = empty ? [] : null,
            ExcludedNavigationalActions = empty ? [] : null
        }];
        var result = Model.DialogByIdMapExtensions.ToDialog(source);
        result.Progress.Should().BeNull();
        result.DueAt.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        return Verify(result).UseParameters(empty).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Parties_Null_Or_Empty_SubParties(bool empty)
    {
        var source = new AuthorizedPartyDto { Party = "party", Name = "name", PartyType = "Person", SubParties = empty ? [] : null };
        var result = Party.PartiesMapExtensions.ToAuthorizedParties(new List<AuthorizedPartyDto> { source });
        return Verify(result).UseParameters(empty).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Mutations_Empty_Labels_And_Null_Revision()
    {
        var single = Mutation.MutationsMapExtensions.ToCommand(new Mutation.SetSystemLabelInput());
        var bulk = Mutation.MutationsMapExtensions.ToCommand(new Mutation.BulkSetSystemLabelInput { Dialogs = [new()] });
        var emptyBulk = Mutation.MutationsMapExtensions.ToCommand(new Mutation.BulkSetSystemLabelInput());
        // Verify omits default scalar values; distinguish null from Guid.Empty explicitly.
        bulk.Dto.Dialogs.Single().EndUserContextRevision.Should().BeNull();
        return Verify(new { Single = single, Bulk = bulk, EmptyBulk = emptyBulk })
            .DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Dialog_Status_All_Values()
    {
        var result = Enum.GetValues<DialogStatus>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToDialog(new Get.DialogDto { Status = value }).Status });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Search_Result_Status_All_Values()
    {
        var result = Enum.GetValues<DialogStatus>()
            .Select(value => new { Source = value, Result = SearchModel.SearchDialogsMapExtensions.ToSearchDialog(new Search.DialogDto { Status = value }).Status });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Actor_Type_All_Values()
    {
        var result = Enum.GetValues<ActorType>()
            .Select(value => new { Source = value, Result = Common.CommonMapExtensions.ToGraphQlActor(new ActorDto { ActorType = value }).ActorType });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Dialog_Activity_Type_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities.DialogActivityType.Values>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToActivity(new Get.DialogActivityDto { Type = value }).Type });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Search_Activity_Type_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities.DialogActivityType.Values>()
            .Select(value => new { Source = value, Result = SearchModel.SearchDialogsMapExtensions.ToActivity(new Search.DialogActivityDto { Type = value }).Type });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Transmission_Type_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmissionType.Values>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToTransmission(new Get.DialogTransmissionDto { Type = value }).Type });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Gui_Action_Priority_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiActionPriority.Values>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToGuiAction(new Get.DialogGuiActionDto { Priority = value, HttpMethod = HttpVerb.GET }).Priority });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Gui_Action_Http_Method_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.Http.HttpVerb.Values>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToGuiAction(new Get.DialogGuiActionDto { HttpMethod = value, Priority = GuiActionPriority.Primary }).HttpMethod });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Api_Endpoint_Http_Method_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.Http.HttpVerb.Values>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToApiActionEndpoint(new Get.DialogApiActionEndpointDto { HttpMethod = value }).HttpMethod });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Attachment_Consumer_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.Attachments.AttachmentUrlConsumerType.Values>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToAttachmentUrl(new Get.DialogAttachmentUrlDto { ConsumerType = value }).ConsumerType });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Transmission_Attachment_Consumer_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.Attachments.AttachmentUrlConsumerType.Values>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToAttachmentUrl(new Get.DialogTransmissionAttachmentUrlDto { ConsumerType = value }).ConsumerType });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Dialog_System_Labels_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel.Values>()
            .Select(value => new { Source = value, Result = Model.DialogByIdMapExtensions.ToEndUserContext(new Get.DialogEndUserContextDto { SystemLabels = [value] }).SystemLabels });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Search_System_Labels_All_Values()
    {
        var result = Enum.GetValues<Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel.Values>()
            .Select(value => new { Source = value, Result = SearchModel.SearchDialogsMapExtensions.ToEndUserContext(new Search.DialogEndUserContextDto { SystemLabels = [value] }).SystemLabels });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Lookup_Grant_Type_All_Values()
    {
        var result = Enum.GetValues<IdentifierLookupGrantType>().Select(value => new
        {
            Source = value,
            Result = Lookup.DialogLookupMapExtensions.ToEvidenceItem(
                new IdentifierLookupAuthorizationEvidenceItemDto { Subject = "subject", GrantType = value }).GrantType
        });
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Search_Status_Filter_All_Values()
    {
        var result = Enum.GetValues<Common.DialogStatus>().Select(value => new
        {
            Source = value,
            Result = SearchModel.SearchDialogsMapExtensions.ToSearchDialogQuery(new SearchModel.SearchDialogInput { Status = [value] }).Status
        });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task System_Label_Inputs_All_Values()
    {
        var result = Enum.GetValues<Common.SystemLabel>().Select(value => new
        {
            Source = value,
            Search = SearchModel.SearchDialogsMapExtensions.ToSearchDialogQuery(new SearchModel.SearchDialogInput { SystemLabel = [value] }).SystemLabel,
            Single = Mutation.MutationsMapExtensions.ToCommand(new Mutation.SetSystemLabelInput { AddLabels = [value], RemoveLabels = [value] }),
            Bulk = Mutation.MutationsMapExtensions.ToCommand(new Mutation.BulkSetSystemLabelInput { AddLabels = [value], RemoveLabels = [value] })
        });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Dialog_And_Search_Boolean_Flags()
    {
        // One flag at a time also detects accidentally assigning a different Boolean property.
        string[] cases = ["AllFalse", "IsApiOnly", "HasUnopenedContent", "IsContentSeen"];
        var result = cases.Select((caseName, index) =>
        {
            var dialog = Model.DialogByIdMapExtensions.ToDialog(new Get.DialogDto
            {
                Status = DialogStatus.NotApplicable,
                IsApiOnly = index == 1,
                HasUnopenedContent = index == 2,
                IsContentSeen = index == 3
            });
            var search = SearchModel.SearchDialogsMapExtensions.ToSearchDialog(new Search.DialogDto
            {
                Status = DialogStatus.NotApplicable,
                IsApiOnly = index == 1,
                HasUnopenedContent = index == 2,
                IsContentSeen = index == 3
            });
            return new
            {
                Case = caseName,
                Dialog = new { dialog.IsApiOnly, dialog.HasUnopenedContent, dialog.IsContentSeen },
                Search = new { search.IsApiOnly, search.HasUnopenedContent, search.IsContentSeen }
            };
        });
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Party_Boolean_Flags()
    {
        string[] cases = ["AllFalse", "IsDeleted", "HasKeyRole", "IsCurrentEndUser", "IsMainAdministrator", "IsAccessManager", "HasOnlyAccessToSubParties"];
        var result = cases.Select((caseName, index) =>
        {
            var source = new AuthorizedPartyDto
            {
                IsDeleted = index == 1,
                HasKeyRole = index == 2,
                IsCurrentEndUser = index == 3,
                IsMainAdministrator = index == 4,
                IsAccessManager = index == 5,
                HasOnlyAccessToSubParties = index == 6
            };
            return new
            {
                Case = caseName,
                Party = Party.PartiesMapExtensions.ToAuthorizedParty(source),
                SubParty = Party.PartiesMapExtensions.ToAuthorizedSubParty(source)
            };
        });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Fact]
    public Task Lookup_Boolean_Flags()
    {
        string[] cases = ["AllFalse", "ViaRoleAndDelegable", "ViaAccessPackage", "ViaResourceDelegation", "ViaInstanceDelegation"];
        var result = cases.Select((caseName, index) =>
        {
            var evidence = new IdentifierLookupAuthorizationEvidenceDto
            {
                ViaRole = index == 1,
                ViaAccessPackage = index == 2,
                ViaResourceDelegation = index == 3,
                ViaInstanceDelegation = index == 4
            };
            return new
            {
                Case = caseName,
                Evidence = Lookup.DialogLookupMapExtensions.ToAuthorizationEvidence(evidence),
                Resource = Lookup.DialogLookupMapExtensions.ToServiceResource(new IdentifierLookupServiceResourceDto
                {
                    Id = "resource",
                    IsDelegable = index == 1,
                    MinimumAuthenticationLevel = 3
                })
            };
        });
        return Verify(result).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Nested_Boolean_Flags(bool value)
    {
        var guiAction = Model.DialogByIdMapExtensions.ToGuiAction(new Get.DialogGuiActionDto { Priority = GuiActionPriority.Primary, HttpMethod = HttpVerb.GET, IsAuthorized = value, IsDeleteDialogAction = !value });
        var transmission = Model.DialogByIdMapExtensions.ToTransmission(new Get.DialogTransmissionDto { Type = TransmissionType.Information, IsAuthorized = value, IsOpened = !value });
        var result = new
        {
            GuiAction = new { guiAction.IsAuthorized, guiAction.IsDeleteDialogAction },
            Transmission = new { transmission.IsAuthorized, transmission.IsOpened },
            ApiActionAuthorized = Model.DialogByIdMapExtensions.ToApiAction(new Get.DialogApiActionDto { IsAuthorized = value }).IsAuthorized,
            AttachmentAuthorized = Model.DialogByIdMapExtensions.ToAttachment(new Get.DialogAttachmentDto { IsAuthorized = value }).IsAuthorized,
            TransmissionAttachmentAuthorized = Model.DialogByIdMapExtensions.ToAttachment(new Get.DialogTransmissionAttachmentDto { IsAuthorized = value }).IsAuthorized,
            NavigationalActionAuthorized = Model.DialogByIdMapExtensions.ToNavigationalAction(new Get.DialogTransmissionNavigationalActionDto { IsAuthorized = value }).IsAuthorized,
            EndpointDeprecated = Model.DialogByIdMapExtensions.ToApiActionEndpoint(new Get.DialogApiActionEndpointDto { HttpMethod = HttpVerb.GET, Deprecated = value }).Deprecated,
            SeenByCurrentEndUser = Model.DialogByIdMapExtensions.ToSeenLog(new Get.DialogSeenLogDto { IsCurrentEndUser = value }).IsCurrentEndUser,
            SearchSeenByCurrentEndUser = SearchModel.SearchDialogsMapExtensions.ToSeenLog(new Search.DialogSeenLogDto { IsCurrentEndUser = value }).IsCurrentEndUser
        };
        return Verify(result).UseParameters(value).UseDirectory("Snapshots");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public Task Nested_Nullable_Boolean_Flags(bool? value)
    {
        var result = new
        {
            Content = Common.CommonMapExtensions.ToGraphQlContentValue(new ContentValueDto { IsAuthorized = value }),
            Seen = Model.DialogByIdMapExtensions.ToSeenLog(new Get.DialogSeenLogDto { IsViaServiceOwner = value }),
            SearchSeen = SearchModel.SearchDialogsMapExtensions.ToSeenLog(new Search.DialogSeenLogDto { IsViaServiceOwner = value })
        };
        return Verify(result).UseParameters(value).DontIgnoreEmptyCollections().UseDirectory("Snapshots");
    }
}
