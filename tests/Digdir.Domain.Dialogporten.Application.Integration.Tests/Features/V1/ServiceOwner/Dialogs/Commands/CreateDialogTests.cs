using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using TransmissionContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionContentDto;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogTests : ApplicationCollectionFixture
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CreateDialogTests(DialogApplication application, ITestOutputHelper testOutputHelper) : base(application)
    {
        _testOutputHelper = testOutputHelper;
    }

    private sealed class CreateDialogWithSpecifiedDialogIdTestData : TheoryData<string, Guid, Type>
    {
        public CreateDialogWithSpecifiedDialogIdTestData()
        {
            Add("Validations for UUIDv4 format",
                Guid.NewGuid(),
                typeof(ValidationError));

            Add("Validations for UUIDv7 format, big endian",
                Guid.Parse("b2ca9301-c371-ab74-a87b-4ee1416b9655"),
                typeof(ValidationError));

            Add("Validations for UUIDv7 with timestamp in the future, with tolerance of 15 seconds",
                IdentifiableExtensions.CreateVersion7(DateTimeOffset.UtcNow.AddSeconds(1)),
                typeof(CreateDialogSuccess));

            Add("Validations for UUIDv7 with timestamp in the future, with tolerance of 15 seconds",
                IdentifiableExtensions.CreateVersion7(DateTimeOffset.UtcNow.AddSeconds(14)),
                typeof(CreateDialogSuccess));

            Add("Validations for UUIDv7 with timestamp in the future, with tolerance of 15 seconds",
                IdentifiableExtensions.CreateVersion7(DateTimeOffset.UtcNow.AddSeconds(16)),
                typeof(ValidationError));

            Add("Can create a dialog with a valid UUIDv7 format",
                IdentifiableExtensions.CreateVersion7(DateTimeOffset.UtcNow.AddSeconds(-1)),
                typeof(CreateDialogSuccess));
        }
    }

    [Theory, ClassData(typeof(CreateDialogWithSpecifiedDialogIdTestData))]
    public Task Create_Dialog_With_Specified_DialogId_Tests(string _, Guid guidInput, Type assertType) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = guidInput)
            .ExecuteAndAssert(assertType);

    [Fact]
    public async Task Creates_Dialog_When_Dialog_Is_Simple()
    {
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = expectedDialogId)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.Id.Should().Be(expectedDialogId));
    }

    [Fact]
    public Task Can_Create_Dialog_With_Empty_Content_Summary() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Content!.Summary = null)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Content!.Summary.Should().BeNull());

    [Fact]
    public async Task Create_Dialog_When_Dialog_Is_Complex()
    {
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();

        await FlowBuilder.For(Application)
            .CreateComplexDialog(x => x.Dto.Id = expectedDialogId)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Id.Should().Be(expectedDialogId));
    }

    private sealed class ValidUpdatedAtTestData : TheoryData<string, DateTimeOffset?, DateTimeOffset>
    {
        public ValidUpdatedAtTestData()
        {
            var someTimeInThePast = DateTimeOffset.UtcNow.AddYears(-15);
            Add("Can create dialog with the same UpdatedAt and CreatedAt",
                someTimeInThePast, // CreatedAt
                someTimeInThePast); // UpdatedAt


            Add("Can create dialog with default UpdatedAt and no CreatedAt",
                null, // CreatedAt
                default); // UpdatedAt

            Add("Can create dialog with default UpdatedAt and CreatedAt",
                default(DateTimeOffset), // CreatedAt
                default); // UpdatedAt
        }
    }

    [Theory, ClassData(typeof(ValidUpdatedAtTestData))]
    public async Task Can_Create_Dialog_With_UpdatedAt_And_CreatedAt_Supplied(string _, DateTimeOffset? createdAt,
        DateTimeOffset updatedAt)
    {
        var dialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.UpdatedAt = updatedAt;
                x.Dto.CreatedAt = createdAt;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>();

        var createdAtHasValue = createdAt.HasValue && createdAt != default(DateTimeOffset);
        dialog.CreatedAt.Should().BeCloseTo(createdAtHasValue ? createdAt!.Value : DateTimeOffset.UtcNow,
            precision: TimeSpan.FromSeconds(1));

        dialog.UpdatedAt.Should().BeCloseTo(updatedAt == default ? DateTimeOffset.UtcNow : updatedAt,
            precision: TimeSpan.FromSeconds(1));
    }

    private sealed class InvalidUpdatedAtTestData : TheoryData<string, DateTimeOffset?, DateTimeOffset>
    {
        public InvalidUpdatedAtTestData()
        {
            // CreatedAt must not be empty when 'UpdatedAt is set.
            Add("Can't create dialog with UpdatedAt without CreatedAt",
                null, // CreatedAt
                DateTimeOffset.UtcNow.AddYears(-15)); // UpdatedAt

            // UpdatedAt before CreatedAt
            Add("Can't create dialog with UpdatedAt before CreatedAt",
                DateTimeOffset.UtcNow.AddYears(-10), // CreatedAt
                DateTimeOffset.UtcNow.AddYears(-15)); // UpdatedAt

            // Can't create dialog with CreatedAt or UpdatedAt in the future
            Add("Can't create dialog with CreatedAt or UpdatedAt in the future",
                DateTimeOffset.UtcNow.AddYears(1), // CreatedAt
                DateTimeOffset.UtcNow.AddYears(1)); // UpdatedAt
        }
    }

    [Theory, ClassData(typeof(InvalidUpdatedAtTestData))]
    public Task Invalid_UpdatedAt_Tests(string _, DateTimeOffset? createdAt, DateTimeOffset updatedAt) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.CreatedAt = createdAt;
                x.Dto.UpdatedAt = updatedAt;
            })
            .ExecuteAndAssert<ValidationError>(x =>
            {
                _testOutputHelper.WriteLine(string.Join(Environment.NewLine, x.Errors.Select(e => e.ErrorMessage)));
                x.ShouldHaveErrorWithText(nameof(updatedAt));
            });

    private sealed class InvalidTransmissionContentTestData : TheoryData<string, TransmissionContentDto?>
    {
        public InvalidTransmissionContentTestData()
        {
            Add("Can't create transmission with null content", null);

            Add("Can't create transmission with empty content",
                new TransmissionContentDto
                {
                    Summary = new(),
                    Title = new()
                });

            Add("Can't create transmission with empty content values",
                new TransmissionContentDto
                {
                    Summary = new() { Value = [new() { Value = "", LanguageCode = "nb" }] },
                    Title = new() { Value = [new() { Value = "", LanguageCode = "nb" }] }
                });
        }
    }

    [Theory, ClassData(typeof(InvalidTransmissionContentTestData))]
    public Task Invalid_Transmission_Content(string _, TransmissionContentDto? content) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.Content = content;
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

    private sealed class HtmlContentTestData : TheoryData<string, Action<IServiceCollection>, Action<CreateDialogCommand>, Type>
    {
        public HtmlContentTestData()
        {
            Add("Cannot create dialog with HTML content without valid html scope",
                _ => { }, // No change in user scopes
                x => x.Dto.Content!.AdditionalInfo = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                typeof(ValidationError));

            Add("Can create dialog with HTML content with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.AdditionalInfo = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                typeof(CreateDialogSuccess));

            Add("Can create HTML content with table tag with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.AdditionalInfo = CreateTableHtml(),
                typeof(CreateDialogSuccess));

            Add("Cannot create dialog with forbidden HTML tags: iframe",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<iframe src='malicious site'></iframe>"),
                typeof(ValidationError));

            Add("Cannot create dialog with forbidden HTML tags: script",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<script>alert('hack');</script>"),
                typeof(ValidationError));

            Add("Cannot create dialog with forbidden HTML tags: img",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<img src='evil.png' />"),
                typeof(ValidationError));

            Add("Cannot create dialog with forbidden HTML tags: div",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<div>Not allowed</div>"),
                typeof(ValidationError));

            Add("Cannot create dialog with forbidden HTML tags: span",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.AdditionalInfo = CreateInvalidHtml("<span>Not allowed</span>"),
                typeof(ValidationError));

            Add("Cannot create title content with HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.Title = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                typeof(ValidationError));

            Add("Cannot create summary content with HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.Summary = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                typeof(ValidationError));

            Add("Cannot create title content with embeddable HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.Title = CreateHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                typeof(ValidationError));

            Add("Can create mainContentRef content with embeddable HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Dto.Content!.MainContentReference = CreateEmbeddableHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                typeof(CreateDialogSuccess));
        }
    }

    [Theory, ClassData(typeof(HtmlContentTestData))]
    public Task Html_Content_Tests(string _, Action<IServiceCollection> appConfig,
        Action<CreateDialogCommand> createDialog, Type expectedType) =>
        FlowBuilder.For(Application, appConfig)
            .CreateSimpleDialog(createDialog)
            .ExecuteAndAssert(expectedType);

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
            .CreateSimpleDialog(x =>
            {
                x.Dto.Transmissions = DialogGenerator.GenerateFakeDialogTransmissions(2);
                x.Dto.Transmissions[0].Type = DialogTransmissionType.Values.Submission;
                x.Dto.Transmissions[0].Sender = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = "Fredrik",
                    ActorId = null
                };
                x.Dto.Transmissions[1].Type = DialogTransmissionType.Values.Submission;
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
            .CreateSimpleDialog(x =>
            {
                x.Dto.IsApiOnly = isApiOnly;
                x.Dto.Content = null;
            })
            .ExecuteAndAssert(expectedType);

    private sealed class TransmissionsCountTestData : TheoryData<string, Action<CreateDialogCommand>, int, int>
    {
        public TransmissionsCountTestData()
        {
            Add("2 From Party, 1 From ServiceOwner",
                x =>
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
                }, 2, 1
            );

            Add("1 From ServiceOwner", x =>
            {
                x.AddTransmission(x =>
                {
                    x.Type = DialogTransmissionType.Values.Information;
                    x.WithServiceOwnerActor();
                });
            }, 0, 1);

            Add("1 From Party", x =>
            {
                x.AddTransmission(x =>
                {
                    x.Type = DialogTransmissionType.Values.Correction;
                    x.WithPartyRepresentativeActor();
                });
            }, 1, 0);
        }
    }

    [Theory, ClassData(typeof(TransmissionsCountTestData))]
    public Task Creating_Dialogs_With_Transmissions_Should_Count_FromServiceOwnerTransmissionsCount_And_FromPartyTransmissionsCount_Correctly(
        string _, Action<CreateDialogCommand> createDialog, int fromPartyCount, int fromServiceOwnerCount) =>
        FlowBuilder.For(Application).CreateSimpleDialog(createDialog)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.FromPartyTransmissionsCount.Should().Be(fromPartyCount);
                x.FromServiceOwnerTransmissionsCount.Should().Be(fromServiceOwnerCount);
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
            .CreateSimpleDialog(x =>
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
        DialogTransmissionType.Values transmissionType, bool shouldAddSentLabel) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.Type = transmissionType))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                if (shouldAddSentLabel)
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

    private sealed class SystemLabelOnDialogCreateTestData : TheoryData<SystemLabel.Values, bool>
    {
        public SystemLabelOnDialogCreateTestData()
        {
            var systemLabels = Enum.GetValues<SystemLabel.Values>();

            foreach (var systemLabel in systemLabels)
            {
                Add(systemLabel, SystemLabel.IsDefaultArchiveBinGroup(systemLabel));
            }

            Add(0, false);
            Add((SystemLabel.Values)systemLabels.Length + 1, false);
        }
    }

    [Theory, ClassData(typeof(SystemLabelOnDialogCreateTestData))]
    public Task SystemLabel_On_Dialog_Create_Should_Be_Accepted_When_In_Default_DAB_Group(
        SystemLabel.Values systemLabel, bool shouldSucceed) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.Dto.SystemLabel = systemLabel)
            .ExecuteAndAssert(result =>
            {
                if (shouldSucceed)
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

    [Fact]
    public Task Can_Create_Dialog_Without_Supplying_Dialog_Status() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Status = null)
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
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.AuthorizationAttribute = authAttribute))
            .ExecuteAndAssert(expectedTye);

    [Fact]
    public Task Supplied_UpdatedAt_Should_Be_Used_For_ContentUpdatedAt() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
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
}
