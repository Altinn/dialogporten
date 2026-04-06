using System.Security.Claims;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using TransmissionContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionContentDto;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogTests : ApplicationCollectionFixture
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CreateDialogTests(DialogApplication application, ITestOutputHelper testOutputHelper) : base(application)
    {
        _testOutputHelper = testOutputHelper;
    }

    public sealed record CreateDialogWithSpecifiedDialogIdScenario(
        string DisplayName,
        Guid DialogId,
        Type ExpectedResultType) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class CreateDialogWithSpecifiedDialogIdTestData : TheoryData<CreateDialogWithSpecifiedDialogIdScenario>
    {
        public static DateTimeOffset FixedUtcNow => new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        public CreateDialogWithSpecifiedDialogIdTestData()
        {
            Add(new CreateDialogWithSpecifiedDialogIdScenario(
                DisplayName: "Validations for UUIDv4 format",
                DialogId: Guid.NewGuid(),
                ExpectedResultType: typeof(ValidationError)));

            Add(new CreateDialogWithSpecifiedDialogIdScenario(
                DisplayName: "Validations for UUIDv7 format, big endian",
                DialogId: Guid.Parse("b2ca9301-c371-ab74-a87b-4ee1416b9655"),
                ExpectedResultType: typeof(ValidationError)));

            Add(new CreateDialogWithSpecifiedDialogIdScenario(
                DisplayName: "UUIDv7 future timestamp +1s should succeed",
                DialogId: IdentifiableExtensions.CreateVersion7(FixedUtcNow.AddSeconds(1)),
                ExpectedResultType: typeof(CreateDialogSuccess)));

            Add(new CreateDialogWithSpecifiedDialogIdScenario(
                DisplayName: "UUIDv7 future timestamp +14s should succeed",
                DialogId: IdentifiableExtensions.CreateVersion7(FixedUtcNow.AddSeconds(14)),
                ExpectedResultType: typeof(CreateDialogSuccess)));

            Add(new CreateDialogWithSpecifiedDialogIdScenario(
                DisplayName: "UUIDv7 future timestamp +16s should fail validation",
                DialogId: IdentifiableExtensions.CreateVersion7(FixedUtcNow.AddSeconds(16)),
                ExpectedResultType: typeof(ValidationError)));

            Add(new CreateDialogWithSpecifiedDialogIdScenario(
                DisplayName: "Can create a dialog with a valid UUIDv7 format",
                DialogId: IdentifiableExtensions.CreateVersion7(FixedUtcNow.AddSeconds(-1)),
                ExpectedResultType: typeof(CreateDialogSuccess)));
        }
    }

    [Theory, ClassData(typeof(CreateDialogWithSpecifiedDialogIdTestData))]
    public Task Create_Dialog_With_Specified_DialogId_Tests(
        CreateDialogWithSpecifiedDialogIdScenario scenario) =>
        FlowBuilder.For(Application)
            .OverrideUtc(CreateDialogWithSpecifiedDialogIdTestData.FixedUtcNow)
            .CreateSimpleDialog((x, _) => x.Dto.Id = scenario.DialogId)
            .ExecuteAndAssert(scenario.ExpectedResultType);

    public sealed record CreateDialogWithSpecifiedCreatedAtScenario(
        string DisplayName,
        DateTimeOffset CreatedAt,
        Type ExpectedResultType) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class CreateDialogWithSpecifiedCreatedAtTestData : TheoryData<CreateDialogWithSpecifiedCreatedAtScenario>
    {
        public static DateTimeOffset FixedUtcNow => new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        public CreateDialogWithSpecifiedCreatedAtTestData()
        {
            Add(new CreateDialogWithSpecifiedCreatedAtScenario(
                DisplayName: "Can create dialog with CreatedAt 1 second in the future, within tolerance",
                CreatedAt: FixedUtcNow.AddSeconds(1),
                ExpectedResultType: typeof(CreateDialogSuccess)));

            Add(new CreateDialogWithSpecifiedCreatedAtScenario(
                DisplayName: "Can create dialog with CreatedAt 14 seconds in the future, within tolerance",
                CreatedAt: FixedUtcNow.AddSeconds(14),
                ExpectedResultType: typeof(CreateDialogSuccess)));

            Add(new CreateDialogWithSpecifiedCreatedAtScenario(
                DisplayName: "Cannot create dialog with CreatedAt 16 seconds in the future, beyond tolerance",
                CreatedAt: FixedUtcNow.AddSeconds(16),
                ExpectedResultType: typeof(ValidationError)));
        }
    }

    [Theory, ClassData(typeof(CreateDialogWithSpecifiedCreatedAtTestData))]
    public Task Create_Dialog_With_Specified_CreatedAt_Tests(
        CreateDialogWithSpecifiedCreatedAtScenario scenario) =>
        FlowBuilder.For(Application)
            .OverrideUtc(CreateDialogWithSpecifiedCreatedAtTestData.FixedUtcNow)
            .CreateSimpleDialog((x, _) => x.Dto.CreatedAt = scenario.CreatedAt)
            .ExecuteAndAssert(scenario.ExpectedResultType);

    public sealed record CreateDialogWithSpecifiedPartyScenario(string DisplayName, string Party) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class CreateDialogWithSpecifiedPartyTestData : TheoryData<CreateDialogWithSpecifiedPartyScenario>
    {
        public CreateDialogWithSpecifiedPartyTestData()
        {
            Add(new CreateDialogWithSpecifiedPartyScenario(
                DisplayName: "Can create dialog with OrganizationIdentifier as Party",
                Party: "urn:altinn:organization:identifier-no:974760673"));

            Add(new CreateDialogWithSpecifiedPartyScenario(
                DisplayName: "Can create dialog with PersonalIdentifier as Party",
                Party: "urn:altinn:person:identifier-no:15915299854"));

            Add(new CreateDialogWithSpecifiedPartyScenario(
                DisplayName: "Can create dialog with A2 SI User as Party",
                Party: "urn:altinn:person:legacy-selfidentified:SOMEUSER"));

            Add(new CreateDialogWithSpecifiedPartyScenario(
                DisplayName: "Can create dialog with ID-Porten email as Party",
                Party: "urn:altinn:person:idporten-email:foo@BAR.com"));

            // Not supported yet
            //Add("Can create dialog with Feide orgsub", "urn:altinn:feide-subject:33a633c47ef2f656978f957532ce6d0de6f5e13f1e0618b37b4b2a70573e5551");
        }
    }

    [Theory, ClassData(typeof(CreateDialogWithSpecifiedPartyTestData))]
    public async Task Creates_Dialog_When_Dialog_Is_Simple_For_All_PartyTypes(
        CreateDialogWithSpecifiedPartyScenario scenario)
    {
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = expectedDialogId;
                x.Dto.Party = scenario.Party;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Id.Should().Be(expectedDialogId);
                x.Party.Should().Be(scenario.Party.ToLowerInvariant());
            });
    }

    [Fact]
    public Task VisibleFrom_Should_Control_Timestamps_On_Create()
    {
        var visibleFrom = DateTimeOffset.UtcNow.AddDays(3);

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.VisibleFrom = visibleFrom)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
            {
                dialog.VisibleFrom.Should().BeCloseTo(visibleFrom, TimeSpan.FromSeconds(1));
                dialog.CreatedAt.Should().Be(dialog.VisibleFrom);
                dialog.UpdatedAt.Should().Be(dialog.VisibleFrom);
                dialog.ContentUpdatedAt.Should().Be(dialog.VisibleFrom);
            });
    }

    [Fact]
    public Task Can_Create_Dialog_With_Empty_Content_Summary() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Content!.Summary = null)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Content!.Summary.Should().BeNull());

    [Fact]
    public async Task Create_Dialog_When_Dialog_Is_Complex()
    {
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();

        await FlowBuilder.For(Application)
            .CreateComplexDialog((x, _) => x.Dto.Id = expectedDialogId)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Id.Should().Be(expectedDialogId));
    }

    public sealed record ValidUpdatedAtScenario(
        string DisplayName,
        DateTimeOffset? CreatedAt,
        DateTimeOffset UpdatedAt) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class ValidUpdatedAtTestData : TheoryData<ValidUpdatedAtScenario>
    {
        public ValidUpdatedAtTestData()
        {
            var someTimeInThePast = DateTimeOffset.UtcNow.AddYears(-15);
            Add(new ValidUpdatedAtScenario(
                DisplayName: "Can create dialog with the same UpdatedAt and CreatedAt",
                CreatedAt: someTimeInThePast,
                UpdatedAt: someTimeInThePast)); // UpdatedAt

            Add(new ValidUpdatedAtScenario(
                DisplayName: "Can create dialog with default UpdatedAt and no CreatedAt",
                CreatedAt: null,
                UpdatedAt: default));

            Add(new ValidUpdatedAtScenario(
                DisplayName: "Can create dialog with default UpdatedAt and CreatedAt",
                CreatedAt: default(DateTimeOffset),
                UpdatedAt: default));
        }
    }

    [Theory, ClassData(typeof(ValidUpdatedAtTestData))]
    public async Task Can_Create_Dialog_With_UpdatedAt_And_CreatedAt_Supplied(ValidUpdatedAtScenario scenario)
    {
        var dialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.UpdatedAt = scenario.UpdatedAt;
                x.Dto.CreatedAt = scenario.CreatedAt;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>();

        var createdAtHasValue = scenario.CreatedAt.HasValue && scenario.CreatedAt != default(DateTimeOffset);
        dialog.CreatedAt.Should().BeCloseTo(createdAtHasValue ? scenario.CreatedAt!.Value : DateTimeOffset.UtcNow,
            precision: TimeSpan.FromSeconds(1));

        dialog.UpdatedAt.Should().BeCloseTo(scenario.UpdatedAt == default ? DateTimeOffset.UtcNow : scenario.UpdatedAt,
            precision: TimeSpan.FromSeconds(1));
    }

    public sealed record InvalidUpdatedAtScenario(
        string DisplayName,
        DateTimeOffset? CreatedAt,
        DateTimeOffset UpdatedAt) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class InvalidUpdatedAtTestData : TheoryData<InvalidUpdatedAtScenario>
    {
        public InvalidUpdatedAtTestData()
        {
            // CreatedAt must not be empty when 'UpdatedAt is set.
            Add(new InvalidUpdatedAtScenario(
                DisplayName: "Can't create dialog with UpdatedAt without CreatedAt",
                CreatedAt: null,
                UpdatedAt: DateTimeOffset.UtcNow.AddYears(-15)));

            // UpdatedAt before CreatedAt
            Add(new InvalidUpdatedAtScenario(
                DisplayName: "Can't create dialog with UpdatedAt before CreatedAt",
                CreatedAt: DateTimeOffset.UtcNow.AddYears(-10),
                UpdatedAt: DateTimeOffset.UtcNow.AddYears(-15)));

            // Can't create dialog with CreatedAt or UpdatedAt in the future
            Add(new InvalidUpdatedAtScenario(
                DisplayName: "Can't create dialog with CreatedAt or UpdatedAt in the future",
                CreatedAt: DateTimeOffset.UtcNow.AddYears(1),
                UpdatedAt: DateTimeOffset.UtcNow.AddYears(1)));
        }
    }

    [Theory, ClassData(typeof(InvalidUpdatedAtTestData))]
    public Task Invalid_UpdatedAt_Tests(InvalidUpdatedAtScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.CreatedAt = scenario.CreatedAt;
                x.Dto.UpdatedAt = scenario.UpdatedAt;
            })
            .ExecuteAndAssert<ValidationError>(x =>
            {
                _testOutputHelper.WriteLine(string.Join(Environment.NewLine, x.Errors.Select(e => e.ErrorMessage)));
                x.ShouldHaveErrorWithText(nameof(scenario.UpdatedAt));
            });

    public sealed record InvalidTransmissionContentScenario(
        string DisplayName,
        TransmissionContentDto? Content) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class InvalidTransmissionContentTestData : TheoryData<InvalidTransmissionContentScenario>
    {
        public InvalidTransmissionContentTestData()
        {
            Add(new InvalidTransmissionContentScenario(
                DisplayName: "Can't create transmission with null content",
                Content: null));

            Add(new InvalidTransmissionContentScenario(
                DisplayName: "Can't create transmission with empty content",
                Content: new TransmissionContentDto
                {
                    Summary = new(),
                    Title = new()
                }));

            Add(new InvalidTransmissionContentScenario(
                DisplayName: "Can't create transmission with empty content values",
                Content: new TransmissionContentDto
                {
                    Summary = new() { Value = [new() { Value = "", LanguageCode = "nb" }] },
                    Title = new() { Value = [new() { Value = "", LanguageCode = "nb" }] }
                }));
        }
    }

    [Theory, ClassData(typeof(InvalidTransmissionContentTestData))]
    public Task Invalid_Transmission_Content(InvalidTransmissionContentScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.Content = scenario.Content;
                x.Dto.Transmissions = [transmission];
            })
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText("empty"));

    private static ContentValueDto CreateInvalidHtml(string html) => new()
    {
        MediaType = MediaTypes.LegacyHtml,
        Value = [new()
        {
            LanguageCode = "nb",
            Value = html
        }]
    };

    private static ContentValueDto CreateTableHtml() => new()
    {
        MediaType = MediaTypes.LegacyHtml,
        Value = [new()
        {
            LanguageCode = "nb",
            Value = """
                    <table>
                      <thead>
                        <tr>
                          <th>table head</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr>
                          <td>tr</td>
                        </tr>
                      </tbody>
                    </table>
                    """
        }]
    };

    public sealed record HtmlContentScenario(
        string DisplayName,
        ClaimsPrincipal User,
        Action<CreateDialogCommand, FlowContext> ModifyCommand,
        Type ExpectedResultType) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class HtmlContentTestData : TheoryData<HtmlContentScenario>
    {
        public HtmlContentTestData()
        {
            var legacyHtmlScopeUser = TestUsers
                .FromDefault()
                .WithScope(AuthorizationScope.LegacyHtmlScope)
                .Build();

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create dialog with HTML content without valid html scope",
                User: TestUsers.FromDefault(), // No change in user scopes
                ModifyCommand: (x, _) => x.Dto.Content!.AdditionalInfo = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Can create dialog with HTML content with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.AdditionalInfo = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                ExpectedResultType: typeof(CreateDialogSuccess)));

            Add(new HtmlContentScenario(
                DisplayName: "Can create HTML content with table tag with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.AdditionalInfo = CreateTableHtml(),
                ExpectedResultType: typeof(CreateDialogSuccess)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create dialog with forbidden HTML tags: iframe",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<iframe src='malicious site'></iframe>"),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create dialog with forbidden HTML tags: script",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<script>alert('hack');</script>"),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create dialog with forbidden HTML tags: img",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<img src='evil.png' />"),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create dialog with forbidden HTML tags: div",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<div>Not allowed</div>"),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create dialog with forbidden HTML tags: span",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<span>Not allowed</span>"),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create title content with HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.Title = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create summary content with HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.Summary = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create title content with embeddable HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.Title = CreateHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Can create mainContentRef content with embeddable HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyCommand: (x, _) => x.Dto.Content!.MainContentReference = CreateEmbeddableHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                ExpectedResultType: typeof(CreateDialogSuccess)));
        }
    }

    [Theory, ClassData(typeof(HtmlContentTestData))]
    public Task Html_Content_Tests(HtmlContentScenario scenario) =>
        FlowBuilder.For(Application)
            .AsUser(scenario.User)
            .CreateSimpleDialog(scenario.ModifyCommand)
            .ExecuteAndAssert(scenario.ExpectedResultType);

    [Fact]
    public Task CreateDialogCommand_Should_Return_Revision() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ExecuteAndAssert<CreateDialogSuccess>(x =>
                x.Revision.Should().NotBeEmpty());

    [Fact]
    public async Task Can_Create_Actors_With_Same_Name_Without_ActorId()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Transmissions = DialogGenerator.GenerateFakeDialogTransmissions(2);
                x.Dto.Transmissions[0].Sender = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = "Fredrik",
                    ActorId = null
                };
                x.Dto.Transmissions[1].Sender = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = "Fredrik",
                    ActorId = null
                };
            })
            .ExecuteAndAssert<CreateDialogSuccess>();

        var actorNames = await Application.GetDbEntities<ActorName>();
        actorNames.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(true, typeof(CreateDialogSuccess))]
    [InlineData(false, typeof(ValidationError))]
    public Task Dialog_With_Empty_Content_Tests(bool isApiOnly, Type expectedType) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.IsApiOnly = isApiOnly;
                x.Dto.Content = null;
            })
            .ExecuteAndAssert(expectedType);

    public sealed record DatesInPastScenario(string DisplayName, Action<CreateDialogCommand, FlowContext> ModifyCommand) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class DatesInPastTestData : TheoryData<DatesInPastScenario>
    {
        public DatesInPastTestData()
        {
            var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

            Add(new DatesInPastScenario(
                DisplayName: "Can create dialog with DueAt in the past when IsSilentUpdate is set or admin scope is present",
                ModifyCommand: (x, _) => x.Dto.DueAt = pastDate));

            Add(new DatesInPastScenario(
                DisplayName: "Can create dialog with ExpiresAt in the past when IsSilentUpdate is set or admin scope is present",
                ModifyCommand: (x, _) => x.Dto.ExpiresAt = pastDate));

            Add(new DatesInPastScenario(
                DisplayName: "Can create dialog with VisibleFrom in the past when IsSilentUpdate is set or admin scope is present",
                ModifyCommand: (x, _) => x.Dto.VisibleFrom = pastDate));
        }
    }

    [Theory, ClassData(typeof(DatesInPastTestData))]
    public Task Can_Create_Dialog_With_Past_Dates_When_Admin_Scope(
        DatesInPastScenario scenario) =>
        FlowBuilder.For(Application)
            .AsAdminUser()
            .CreateSimpleDialog(scenario.ModifyCommand)
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Theory, ClassData(typeof(DatesInPastTestData))]
    public Task Can_Create_Dialog_With_Past_Dates_When_Silent_Update(
        DatesInPastScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, ctx) =>
            {
                x.IsSilentUpdate = true;
                scenario.ModifyCommand(x, ctx);
            })
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Theory, ClassData(typeof(DatesInPastTestData))]
    public Task Cannot_Create_Dialog_With_Past_Dates_Without_Silent_Update_Or_Admin_Scope(
        DatesInPastScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(scenario.ModifyCommand)
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText("must be in the future"));

    public sealed record TransmissionsCountScenario(
        string DisplayName,
        Action<CreateDialogCommand, FlowContext> CreateDialog,
        int ExpectedFromPartyTransmissions,
        int ExpectedFromServiceOwnerTransmissions) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class TransmissionsCountTestData : TheoryData<TransmissionsCountScenario>
    {
        public TransmissionsCountTestData()
        {
            Add(new TransmissionsCountScenario(
                DisplayName: "2 From Party, 1 From ServiceOwner",
                CreateDialog: (x, _) =>
                {
                    x.AddTransmission(x =>
                    {
                        x.Type = DialogTransmissionType.Values.Submission;
                        x.WithPartyRepresentativeActor();
                    });

                    x.AddTransmission(x =>
                    {
                        x.Type = DialogTransmissionType.Values.Correction;
                        x.WithPartyRepresentativeActor();
                    });
                    x.AddTransmission(x =>
                    {
                        x.Type = DialogTransmissionType.Values.Alert;
                        x.WithServiceOwnerActor();
                    });
                },
                ExpectedFromPartyTransmissions: 2,
                ExpectedFromServiceOwnerTransmissions: 1));

            Add(new TransmissionsCountScenario(
                DisplayName: "1 From ServiceOwner",
                CreateDialog: (x, _) =>
                {
                    x.AddTransmission(x =>
                    {
                        x.Type = DialogTransmissionType.Values.Information;
                        x.WithServiceOwnerActor();
                    });
                },
                ExpectedFromPartyTransmissions: 0,
                ExpectedFromServiceOwnerTransmissions: 1));

            Add(new TransmissionsCountScenario(
                DisplayName: "1 From Party",
                CreateDialog: (x, _) =>
                {
                    x.AddTransmission(x =>
                    {
                        x.Type = DialogTransmissionType.Values.Correction;
                        x.WithPartyRepresentativeActor();
                    });
                },
                ExpectedFromPartyTransmissions: 1,
                ExpectedFromServiceOwnerTransmissions: 0));
        }
    }

    [Theory, ClassData(typeof(TransmissionsCountTestData))]
    public Task Creating_Dialogs_With_Transmissions_Should_Count_FromServiceOwnerTransmissionsCount_And_FromPartyTransmissionsCount_Correctly(
        TransmissionsCountScenario scenario) =>
        FlowBuilder.For(Application).CreateSimpleDialog(scenario.CreateDialog)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.FromPartyTransmissionsCount.Should().Be(scenario.ExpectedFromPartyTransmissions);
                x.FromServiceOwnerTransmissionsCount.Should().Be(scenario.ExpectedFromServiceOwnerTransmissions);
            });

    [Fact]
    public Task Creating_Dialog_Should_Set_ContentUpdatedAt() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.ContentUpdatedAt
                    .Should()
                    .Be(x.CreatedAt));

    [Fact]
    public async Task Dialog_Has_Unopened_Content()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.Type = DialogTransmissionType.Values.Information))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.HasUnopenedContent.Should().BeTrue();
                x.Transmissions.First().IsOpened.Should().BeFalse();
            });
    }

    [Theory, ClassData(typeof(AddingEndUserTransmissionSentLabelTestData))]
    public Task Adding_EndUser_Transmission_Adds_Sent_Label_If_Submission_Or_Correction(
        AddingEndUserTransmissionSentLabelScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.Type = scenario.TransmissionType))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                if (scenario.ShouldAddSentLabel)
                {
                    x.EndUserContext.SystemLabels.Should().ContainSingle(
                        label => label == SystemLabel.Values.Sent);
                }
                else
                {
                    x.EndUserContext.SystemLabels.Should().NotContain(
                        label => label == SystemLabel.Values.Sent);
                }
            });

    public sealed record SystemLabelOnDialogCreateScenario(
        string DisplayName,
        SystemLabel.Values Label,
        bool ShouldSucceed) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class SystemLabelOnDialogCreateTestData : TheoryData<SystemLabelOnDialogCreateScenario>
    {
        public SystemLabelOnDialogCreateTestData()
        {
            var systemLabels = Enum.GetValues<SystemLabel.Values>();

            foreach (var systemLabel in systemLabels)
            {
                Add(new SystemLabelOnDialogCreateScenario(
                    DisplayName: $"SystemLabel {systemLabel} should be default archive bin group: {SystemLabel.IsDefaultArchiveBinGroup(systemLabel)}",
                    Label: systemLabel,
                    ShouldSucceed: SystemLabel.IsDefaultArchiveBinGroup(systemLabel)));
            }

            Add(new SystemLabelOnDialogCreateScenario(
                DisplayName: "Default value outside enum should be allowed",
                Label: 0,
                ShouldSucceed: false));

            Add(new SystemLabelOnDialogCreateScenario(
                DisplayName: "Value beyond enum range should be rejected",
                Label: (SystemLabel.Values)systemLabels.Length + 1,
                ShouldSucceed: false));
        }
    }

    [Theory, ClassData(typeof(SystemLabelOnDialogCreateTestData))]
    public Task SystemLabel_On_Dialog_Create_Should_Be_Accepted_When_In_Default_DAB_Group(
        SystemLabelOnDialogCreateScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.Dto.SystemLabel = scenario.Label)
            .ExecuteAndAssert(result =>
            {
                if (scenario.ShouldSucceed)
                {
                    (result is CreateDialogSuccess)
                        .Should().BeTrue();
                }
                else
                {
                    (result is ValidationError)
                        .Should().BeTrue();
                }
            });

    [Theory]
    [InlineData(DialogStatusInput.New, DialogStatus.Values.NotApplicable)]
    [InlineData(DialogStatusInput.Sent, DialogStatus.Values.Awaiting)]
    public Task Can_Create_Dialog_With_Deprecated_DialogStatus(
        DialogStatusInput initialStatus, DialogStatus.Values expectedStatus) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Status = initialStatus)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.Status.Should().Be(expectedStatus));

    [Fact]
    public Task Can_Create_Dialog_Without_Supplying_Dialog_Status() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Status = null)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Status.Should().Be(DialogStatus.Values.NotApplicable));

    [Theory]
    [InlineData(null, typeof(CreateDialogSuccess))]
    [InlineData("element1", typeof(CreateDialogSuccess))]
    [InlineData("this_is_valid", typeof(CreateDialogSuccess))]
    [InlineData("this-is-valid", typeof(CreateDialogSuccess))]
    [InlineData("urn:altinn:this:is--valid__", typeof(CreateDialogSuccess))]
    [InlineData("urn:dialogporten:invalid:uri", typeof(ValidationError))]
    [InlineData("this:is:invalid", typeof(ValidationError))]
    [InlineData("this.is.invalid", typeof(ValidationError))]
    [InlineData("", typeof(ValidationError))]
    [InlineData("    ", typeof(ValidationError))]
    public Task Create_With_AuthorizationAttribute(string? authAttribute, Type expectedTye) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.AuthorizationAttribute = authAttribute))
            .ExecuteAndAssert(expectedTye);

    [Fact]
    public Task Supplied_UpdatedAt_Should_Be_Used_For_ContentUpdatedAt() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.Dto.UpdatedAt = x.Dto.CreatedAt = DateTimeOffset.UtcNow.AddDays(-1))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.ContentUpdatedAt.Should().Be(x.UpdatedAt));

    [Fact]
    public Task ContentUpdatedAt_Should_Default_To_Now_If_UpdatedAt_Not_Supplied() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                Assert.True(
                    x.ContentUpdatedAt == x.UpdatedAt &&
                    x.ContentUpdatedAt == x.CreatedAt));

    [Theory, ClassData(typeof(DialogContentLengthTestData))]
    public Task Content_Length_Validation_Test(DialogContentLengthScenario scenario) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(scenario.ModifyCommand)
            .ExecuteAndAssert(scenario.ExpectedResultType);

    public sealed record DialogContentLengthScenario(
        string DisplayName,
        Action<CreateDialogCommand, FlowContext> ModifyCommand,
        Type ExpectedResultType) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class DialogContentLengthTestData : TheoryData<DialogContentLengthScenario>
    {
        private static string Repeat(char c, int x) => new(c, x);
        private static int GetMaxLength(DialogContentType.Values value) =>
            DialogContentType.GetValue(value).MaxLength;

        public DialogContentLengthTestData()
        {
            AddLengthTests((x, value) => x.Dto.Content!.Title = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.Title));

            AddLengthTests((x, value) => x.Dto.Content!.SenderName = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.SenderName));

            AddLengthTests((x, value) => x.Dto.Content!.Summary = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.Summary));

            AddLengthTests((x, value) => x.Dto.Content!.AdditionalInfo = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.AdditionalInfo));

            AddLengthTests((x, value) => x.Dto.Content!.ExtendedStatus = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.ExtendedStatus));

            AddLengthTests((x, value) => x.Dto.Content!.NonSensitiveTitle = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.NonSensitiveTitle));

            AddLengthTests((x, value) => x.Dto.Content!.NonSensitiveSummary = CreateContentDto(value),
                GetMaxLength(DialogContentType.Values.NonSensitiveSummary));
        }

        private void AddLengthTests(Action<CreateDialogCommand, string> applyValue, int maxLength)
        {
            Add(new DialogContentLengthScenario(
                DisplayName: $"Valid dialog content ({maxLength} chars)",
                ModifyCommand: (x, _) => applyValue(x, Repeat('x', maxLength)),
                ExpectedResultType: typeof(CreateDialogSuccess)));

            Add(new DialogContentLengthScenario(
                DisplayName: $"Too long dialog content ({maxLength + 1} chars)",
                ModifyCommand: (x, _) => applyValue(x, Repeat('x', maxLength + 1)),
                ExpectedResultType: typeof(ValidationError)));
        }
    }

    private static ContentValueDto CreateContentDto(string content) => new()
    {
        Value = [new() { Value = content, LanguageCode = "nb" }]
    };
}
