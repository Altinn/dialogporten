using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateRelation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.DeleteRelation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using NSubstitute;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using CreateDialogRelationDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.DialogRelationDto;
using EndUserRelationDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchRelations.DialogRelationDto;
using EndUserSearchRelationsQuery = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchRelations.SearchDialogRelationsQuery;
using GetDialogQuerySO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.GetDialogQuery;
using DialogDtoSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;
using ServiceOwnerRelationDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchRelations.DialogRelationDto;
using ServiceOwnerSearchRelationsQuery = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchRelations.SearchDialogRelationsQuery;
using Type = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations.DialogRelationType.Values;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DialogRelationTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Relation_Is_Stored_Once_And_Seen_Inverted_From_The_Other_Side()
    {
        var parent = await CreateDialog();
        var child = await CreateDialog((parent, Type.Parent));

        (await Application.GetDbEntities<DialogRelation>())
            .Where(x => x.DialogId == parent || x.RelatedDialogId == parent)
            .Should().ContainSingle();

        (await GetRelations(child)).Should().BeEquivalentTo([new ServiceOwnerRelationDto { RelatedDialogId = parent, Type = Type.Parent }]);
        (await GetRelations(parent)).Should().BeEquivalentTo([new ServiceOwnerRelationDto { RelatedDialogId = child, Type = Type.Child }]);
    }

    [Theory]
    [InlineData(Type.Parent)]
    [InlineData(Type.Child)]
    [InlineData(Type.Related)]
    public async Task Relation_Type_Is_Kept_Whichever_Dialog_Has_The_Lower_Id(Type type)
    {
        // The first dialog has the lower id, so creating the relation from the second one stores it inverted.
        var first = await CreateDialog();
        var second = await CreateDialog();

        (await AddRelation(second, first, type)).Value.Should().BeOfType<CreateDialogRelationSuccess>();

        (await GetRelations(second)).Should().BeEquivalentTo([new ServiceOwnerRelationDto { RelatedDialogId = first, Type = type }]);
        (await GetRelations(first)).Should().BeEquivalentTo([new ServiceOwnerRelationDto { RelatedDialogId = second, Type = type.Invert() }]);
    }

    [Fact]
    public async Task HasRelations_Is_Set_On_Both_Dialogs_But_Only_The_Through_Dialog_Is_Bumped()
    {
        var first = await CreateDialog();
        var second = await CreateDialog();
        var firstBefore = await GetServiceOwnerDialog(first);
        var secondBefore = await GetServiceOwnerDialog(second);
        Application.PopPublishedEvents();

        (await AddRelation(first, second, Type.Related)).Value.Should().BeOfType<CreateDialogRelationSuccess>();

        var firstAfter = await GetServiceOwnerDialog(first);
        var secondAfter = await GetServiceOwnerDialog(second);
        firstAfter.HasRelations.Should().BeTrue();
        secondAfter.HasRelations.Should().BeTrue();

        firstAfter.Revision.Should().NotBe(firstBefore.Revision);
        firstAfter.UpdatedAt.Should().BeAfter(firstBefore.UpdatedAt);
        firstAfter.ContentUpdatedAt.Should().Be(firstBefore.ContentUpdatedAt);

        secondAfter.Revision.Should().Be(secondBefore.Revision);
        secondAfter.UpdatedAt.Should().Be(secondBefore.UpdatedAt);

        Application.PopPublishedEvents()
            .OfType<DialogUpdatedDomainEvent>()
            .Should().ContainSingle()
            .Which.DialogId.Should().Be(first);
    }

    [Fact]
    public async Task Initial_Relations_On_Create_Do_Not_Bump_The_Related_Dialog()
    {
        var parent = await CreateDialog();
        var parentBefore = await GetServiceOwnerDialog(parent);
        Application.PopPublishedEvents();

        var child = await CreateDialog((parent, Type.Parent));

        var parentAfter = await GetServiceOwnerDialog(parent);
        parentAfter.HasRelations.Should().BeTrue();
        parentAfter.Revision.Should().Be(parentBefore.Revision);
        (await GetServiceOwnerDialog(child)).HasRelations.Should().BeTrue();

        var events = Application.PopPublishedEvents();
        events.OfType<DialogUpdatedDomainEvent>().Should().BeEmpty();
        events.OfType<DialogCreatedDomainEvent>().Should().ContainSingle().Which.DialogId.Should().Be(child);
    }

    [Fact]
    public async Task Self_Relation_Is_Rejected()
    {
        var dialog = await CreateDialog();

        AssertDomainError(await AddRelation(dialog, dialog, Type.Related), "itself");
    }

    [Fact]
    public async Task Relation_To_Unknown_Or_Deleted_Dialog_Is_Rejected()
    {
        var dialog = await CreateDialog();
        var deleted = await CreateDialog();
        (await Application.Send(new DeleteDialogCommand { Id = deleted }, Ct)).Value.Should().BeOfType<DeleteDialogSuccess>();

        AssertDomainError(await AddRelation(dialog, NewUuidV7(), Type.Related), "does not exist");
        AssertDomainError(await AddRelation(dialog, deleted, Type.Related), "does not exist");
    }

    [Fact]
    public async Task Second_Relation_Between_The_Same_Dialogs_Is_Rejected()
    {
        var first = await CreateDialog();
        var second = await CreateDialog();
        (await AddRelation(first, second, Type.Related)).Value.Should().BeOfType<CreateDialogRelationSuccess>();

        AssertDomainError(await AddRelation(second, first, Type.Parent), "already related");
    }

    [Fact]
    public async Task Second_Parent_Is_Rejected()
    {
        var parent = await CreateDialog();
        var otherParent = await CreateDialog();
        var child = await CreateDialog((parent, Type.Parent));

        AssertDomainError(await AddRelation(child, otherParent, Type.Parent), "at most one parent");
        AssertDomainError(await AddRelation(otherParent, child, Type.Child), "at most one parent");

        var createResult = await SendCreate((parent, Type.Parent), (otherParent, Type.Parent));
        AssertDomainError(createResult.Value, "at most one parent");
    }

    [Fact]
    public async Task Grandchildren_Are_Allowed_But_Great_Grandchildren_Are_Not()
    {
        var root = await CreateDialog();
        var child = await CreateDialog((root, Type.Parent));
        var grandchild = await CreateDialog((child, Type.Parent));

        AssertDomainError((await SendCreate((grandchild, Type.Parent))).Value, "levels deep");

        // Attaching an existing two-level hierarchy under a dialog with a parent would also be too deep.
        var otherRoot = await CreateDialog();
        var otherChild = await CreateDialog((otherRoot, Type.Parent));
        AssertDomainError(await AddRelation(root, otherChild, Type.Parent), "levels deep");
        AssertDomainError(await AddRelation(otherChild, root, Type.Child), "levels deep");

        // A dialog with a parent can still get children, as long as the depth stays within the limit.
        var leaf = await CreateDialog();
        (await AddRelation(child, leaf, Type.Child)).Value.Should().BeOfType<CreateDialogRelationSuccess>();
    }

    [Fact]
    public async Task Cycles_Are_Rejected()
    {
        var root = await CreateDialog();
        var child = await CreateDialog((root, Type.Parent));

        AssertDomainError(await AddRelation(root, child, Type.Parent), "already related");

        var grandchild = await CreateDialog((child, Type.Parent));
        AssertDomainError(await AddRelation(root, grandchild, Type.Parent), "descendant");
    }

    [Fact]
    public async Task Relation_Cap_Is_Enforced()
    {
        var dialog = await CreateDialog();
        var others = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            others.Add(await CreateDialog());
        }

        SetMaxRelations(2);
        try
        {
            (await AddRelation(dialog, others[0], Type.Related)).Value.Should().BeOfType<CreateDialogRelationSuccess>();
            (await AddRelation(dialog, others[1], Type.Related)).Value.Should().BeOfType<CreateDialogRelationSuccess>();
            AssertDomainError(await AddRelation(dialog, others[2], Type.Related), "at most 2 relations");
            AssertDomainError(await AddRelation(others[2], dialog, Type.Related), "maximum of 2 relations");
        }
        finally
        {
            DialogApplication.Settings.Reset();
        }
    }

    [Fact]
    public async Task Either_Side_Can_Remove_The_Relation_And_Flags_Are_Cleared()
    {
        var first = await CreateDialog();
        var second = await CreateDialog();
        var third = await CreateDialog();
        (await AddRelation(first, second, Type.Related)).Value.Should().BeOfType<CreateDialogRelationSuccess>();
        (await AddRelation(first, third, Type.Child)).Value.Should().BeOfType<CreateDialogRelationSuccess>();
        var thirdBefore = await GetServiceOwnerDialog(third);

        (await RemoveRelation(third, first)).Value.Should().BeOfType<DeleteDialogRelationSuccess>();
        (await RemoveRelation(third, first)).Value.Should().BeAssignableTo<EntityNotFound>();

        (await GetServiceOwnerDialog(first)).HasRelations.Should().BeTrue();
        var thirdAfter = await GetServiceOwnerDialog(third);
        thirdAfter.HasRelations.Should().BeFalse();
        thirdAfter.Revision.Should().NotBe(thirdBefore.Revision);

        (await RemoveRelation(second, first)).Value.Should().BeOfType<DeleteDialogRelationSuccess>();
        (await GetServiceOwnerDialog(first)).HasRelations.Should().BeFalse();
        (await GetServiceOwnerDialog(second)).HasRelations.Should().BeFalse();
    }

    [Fact]
    public async Task Purge_Removes_Relations_And_Clears_Flags_Without_Bumping()
    {
        var parent = await CreateDialog();
        var onlyChild = await CreateDialog((parent, Type.Parent));
        var other = await CreateDialog();
        (await AddRelation(other, onlyChild, Type.Related)).Value.Should().BeOfType<CreateDialogRelationSuccess>();
        (await AddRelation(other, parent, Type.Related)).Value.Should().BeOfType<CreateDialogRelationSuccess>();
        var otherBefore = await GetServiceOwnerDialog(other);

        (await Application.Send(new PurgeDialogCommand { DialogId = parent }, Ct)).Value.Should().BeOfType<OneOf.Types.Success>();

        (await GetServiceOwnerDialog(onlyChild)).HasRelations.Should().BeTrue();
        var otherAfter = await GetServiceOwnerDialog(other);
        otherAfter.HasRelations.Should().BeTrue();
        otherAfter.Revision.Should().Be(otherBefore.Revision);

        (await Application.Send(new PurgeDialogCommand { DialogId = other }, Ct)).Value.Should().BeOfType<OneOf.Types.Success>();
        (await GetServiceOwnerDialog(onlyChild)).HasRelations.Should().BeFalse();
        (await Application.GetDbEntities<DialogRelation>()).Should().BeEmpty();
    }

    [Fact]
    public async Task Relations_Are_Paginated_Across_Both_Sides_In_Id_Order()
    {
        // Every other dialog is created before the anchor, so the relations are stored on both sides.
        var before = new List<Guid> { await CreateDialog(), await CreateDialog() };
        var anchor = await CreateDialog();
        var after = new List<Guid> { await CreateDialog(), await CreateDialog(), await CreateDialog() };
        foreach (var other in before.Concat(after))
        {
            (await AddRelation(anchor, other, Type.Related)).Value.Should().BeOfType<CreateDialogRelationSuccess>();
        }

        var collected = new List<Guid>();
        string? token = null;
        do
        {
            var page = (await Application.Send(new ServiceOwnerSearchRelationsQuery { DialogId = anchor, Limit = 2, ContinuationToken = token }, Ct))
                .Value.Should().BeOfType<PaginatedList<ServiceOwnerRelationDto>>().Subject;
            collected.AddRange(page.Items.Select(x => x.RelatedDialogId));
            token = page.HasNextPage ? page.ContinuationToken : null;
        } while (token is not null);

        collected.Should().Equal(before.Concat(after));
    }

    [Fact]
    public async Task EndUser_Relations_Only_Include_Authorized_And_Visible_Dialogs()
    {
        const string serviceResource = "urn:altinn:resource:test-relations";
        const string otherServiceResource = "urn:altinn:resource:test-relations-hidden";
        var parent = await CreateDialog(configure: x => x.ServiceResource = serviceResource);
        var visible = await CreateDialog(configure: x => x.ServiceResource = serviceResource, relations: (parent, Type.Parent));
        await CreateDialog(configure: x => x.ServiceResource = otherServiceResource, relations: (parent, Type.Parent));
        await CreateDialog(configure: x =>
        {
            x.ServiceResource = serviceResource;
            x.VisibleFrom = DateTimeOffset.UtcNow.AddDays(1);
        }, relations: (parent, Type.Parent));
        var apiOnly = await CreateDialog(configure: x =>
        {
            x.ServiceResource = serviceResource;
            x.IsApiOnly = true;
        }, relations: (parent, Type.Parent));

        DialogApplication.AltinnAuthorization.Configure(x =>
        {
            x.ConfigureGetAuthorizedResourcesForSearch(new DialogSearchAuthorizationResult
            {
                ResourcesByParties = new Dictionary<string, IReadOnlySet<string>>
                {
                    { TestUsers.DefaultParty, new HashSet<string> { serviceResource } }
                }
            });
            x.HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>()).Returns(true);
        });

        var all = await GetEndUserRelations(parent);
        all.Items.Select(x => x.Dialog.Id).Should().BeEquivalentTo([visible, apiOnly]);
        all.Items.Should().AllSatisfy(x => x.RelationType.Should().Be(Type.Child));
        all.Items.Single(x => x.Dialog.Id == visible).Dialog.Content.Title.Should().NotBeNull();

        var withoutApiOnly = await GetEndUserRelations(parent, excludeApiOnly: true);
        withoutApiOnly.Items.Select(x => x.Dialog.Id).Should().Equal(visible);

        var parents = await GetEndUserRelations(visible, Type.Parent);
        parents.Items.Should().ContainSingle().Which.Dialog.Id.Should().Be(parent);
        (await GetEndUserRelations(visible, Type.Child)).Items.Should().BeEmpty();
    }

    private static void SetMaxRelations(int max)
    {
        DialogApplication.Settings.Set(TestApplicationSettings.CreateDefault(
            limits: new LimitsSettings { DialogRelations = new DialogRelationLimits { MaxRelationsPerDialog = max } }));
    }

    private static void AssertDomainError(object result, string expectedText)
    {
        var domainError = (result is OneOf.IOneOf oneOf ? oneOf.Value : result).Should().BeOfType<DomainError>().Subject;
        domainError.ShouldHaveErrorWithText(expectedText);
    }

    private Task<CreateDialogRelationResult> AddRelation(Guid dialogId, Guid relatedDialogId, Type type) =>
        Application.Send(new CreateDialogRelationCommand { DialogId = dialogId, RelatedDialogId = relatedDialogId, Type = type }, Ct);

    private Task<DeleteDialogRelationResult> RemoveRelation(Guid dialogId, Guid relatedDialogId) =>
        Application.Send(new DeleteDialogRelationCommand { DialogId = dialogId, RelatedDialogId = relatedDialogId }, Ct);

    private async Task<List<ServiceOwnerRelationDto>> GetRelations(Guid dialogId)
    {
        var result = await Application.Send(new ServiceOwnerSearchRelationsQuery { DialogId = dialogId }, Ct);
        return result.Value.Should().BeOfType<PaginatedList<ServiceOwnerRelationDto>>().Subject.Items;
    }

    private async Task<PaginatedList<EndUserRelationDto>> GetEndUserRelations(Guid dialogId, Type? type = null, bool? excludeApiOnly = null)
    {
        var result = await Application.Send(new EndUserSearchRelationsQuery { DialogId = dialogId, Type = type, ExcludeApiOnly = excludeApiOnly }, Ct);
        return result.Value.Should().BeOfType<PaginatedList<EndUserRelationDto>>().Subject;
    }

    private Task<CreateDialogResult> SendCreate(params (Guid RelatedDialogId, Type Type)[] relations) =>
        SendCreate(null, relations);

    private Task<CreateDialogResult> SendCreate(Action<CreateDialogDto>? configure, params (Guid RelatedDialogId, Type Type)[] relations)
    {
        var command = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(NewUuidV7());
        command.Dto.Party = TestUsers.DefaultParty;
        command.Dto.Relations = relations
            .Select(x => new CreateDialogRelationDto { RelatedDialogId = x.RelatedDialogId, Type = x.Type })
            .ToList();
        configure?.Invoke(command.Dto);
        return Application.Send(command, Ct);
    }

    private Task<Guid> CreateDialog(params (Guid RelatedDialogId, Type Type)[] relations) =>
        CreateDialog(null, relations);

    private async Task<Guid> CreateDialog(Action<CreateDialogDto>? configure, params (Guid RelatedDialogId, Type Type)[] relations)
    {
        var result = await SendCreate(configure, relations);
        return result.Value.Should().BeOfType<CreateDialogSuccess>().Subject.DialogId;
    }

    private async Task<DialogDtoSO> GetServiceOwnerDialog(Guid dialogId)
    {
        var result = await Application.Send(new GetDialogQuerySO { DialogId = dialogId }, Ct);
        return result.Value.Should().BeOfType<DialogDtoSO>().Subject;
    }
}
