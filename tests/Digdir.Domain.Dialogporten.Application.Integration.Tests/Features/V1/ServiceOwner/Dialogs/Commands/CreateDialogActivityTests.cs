using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using ActivityDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetActivity.ActivityDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogActivityTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Can_Create_Activity_On_Existing_Dialog()
    {
        var guid = Guid.Parse("019c0f25-9759-70c5-8d9d-f03f336a0b6f");
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>()
            .CreateActivity((c, _) => c.Activity = new CreateActivityDto
            {
                Id = guid,
                CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                ExtendedType = new Uri("https://altinn.no"),
                Type = DialogActivityType.Values.DialogCreated,
                TransmissionId = null,
                PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = null,
                    ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                },
                Description = []
            })
            .ExecuteAndAssert<CreateActivitySuccess>(x => x.ActivityId.Should().Be(guid));
    }

    [Fact]
    public Task Can_Create_Activity_On_Existing_Dialog_Without_Supplying_Id()
    {
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>()
            .CreateActivity((c, _) => c.Activity = new CreateActivityDto
            {
                Id = null,
                CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                ExtendedType = new Uri("https://altinn.no"),
                Type = DialogActivityType.Values.DialogCreated,
                TransmissionId = null,
                PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = null,
                    ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                },
                Description = []
            })
            .ExecuteAndAssert<CreateActivitySuccess>(x => x.ActivityId.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public Task Can_Create_Activity_On_Existing_Dialog_And_Get_The_Activity_Afterwards()
    {
        var guid = Guid.Parse("019c0f25-9759-70c5-8d9d-f03f336a0b6f");
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>()
            .CreateActivity((c, _) => c.Activity = new CreateActivityDto
            {
                Id = guid,
                CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                ExtendedType = new Uri("https://altinn.no"),
                Type = DialogActivityType.Values.DialogCreated,
                TransmissionId = null,
                PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = null,
                    ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                },
                Description = []
            })
            .GetActivity()
            .ExecuteAndAssert<ActivityDto>(x =>
                {
                    x.Id.Should().Be(guid);
                    x.CreatedAt.Should().BeBefore(DateTimeOffset.UtcNow);
                    x.ExtendedType.Should().Be(new Uri("https://altinn.no"));
                    x.Type.Should().Be(DialogActivityType.Values.DialogCreated);
                    x.TransmissionId.Should().Be(null);
                    x.PerformedBy.Should().BeEquivalentTo(new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorName = "Brando Sando",
                        ActorId = "urn:altinn:person:legacy-selfidentified:leif"
                    });
                    x.Description.Should().BeNull();
                }
            );
    }

    [Fact]
    public Task Create_Activity_Always_Restores_Dialog_System_Label()
    {
        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.SystemLabel = SystemLabel.Values.Bin)
            .AssertResult<CreateDialogSuccess>()
            .CreateSimpleActivity()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                {
                    x.EndUserContext.SystemLabels.Should().BeEquivalentTo([SystemLabel.Values.Default]);
                }
            );
    }

    [Fact]
    public Task Can_Not_Create_Activity_On_Unknown_Dialog()
    {
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>()
            .SendCommand(ctx => new CreateActivityCommand
            {
                DialogId = Guid.Parse("019c2345-514f-7d6d-9a56-ba272874429e"),
                IfMatchDialogRevision = null,
                Activity = new CreateActivityDto
                {
                    Id = Guid.Parse("019c0f25-9759-70c5-8d9d-f03f336a0b6f"),
                    CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                    ExtendedType = null,
                    Type = DialogActivityType.Values.DialogCreated,
                    TransmissionId = null,
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorName = null,
                        ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                    },
                    Description = []
                },
                IsSilentUpdate = false
            })
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>(x =>
                {
                    x.Name.Should().Be("DialogEntity");
                    x.Message.Should()
                        .Be(
                            "Entity 'DialogEntity' with the following key(s) was not found: (019c2345-514f-7d6d-9a56-ba272874429e).");
                }
            );
    }

    [Fact]
    public Task Can_Not_Create_Activity_On_Deleted_Dialog()
    {
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .CreateActivity(
                (c, _) => c.Activity = new CreateActivityDto
                {
                    Id = Guid.Parse("019c0f25-9759-70c5-8d9d-f03f336a0b6f"),
                    CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                    ExtendedType = null,
                    Type = DialogActivityType.Values.DialogCreated,
                    TransmissionId = null,
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorName = null,
                        ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                    },
                    Description = []
                }
            )
            .ExecuteAndAssert<EntityDeleted<DialogEntity>>((x, ctx) =>
                {
                    x.Name.Should().Be("DialogEntity");
                    var id = ctx.GetDialogId();
                    x.Message.Should().Be($"Entity 'DialogEntity' with the following key(s) is removed: ({id}).");
                }
            );
    }

    [Fact]
    public Task Can_Create_Activity_On_Deleted_Dialog_When_Admin()
    {
        var guid = Guid.Parse("019c89bc-413f-7bb1-ae9f-9c6225c4d9c5");
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .AsAdminUser()
            .CreateActivity(
                (c, _) => c.Activity = new CreateActivityDto
                {
                    Id = guid,
                    CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                    ExtendedType = null,
                    Type = DialogActivityType.Values.DialogCreated,
                    TransmissionId = null,
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorName = null,
                        ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                    },
                    Description = []
                }
            )
            .ExecuteAndAssert<CreateActivitySuccess>(x => x.ActivityId.Should().Be(guid));
    }

    [Fact]
    public Task Can_Not_Create_Activity_On_Unknown_TransmissionId()
    {
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateActivity(
                (c, _) => c.Activity = new CreateActivityDto
                {
                    Id = Guid.Parse("019c0f25-9759-70c5-8d9d-f03f336a0b6f"),
                    CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                    ExtendedType = null,
                    Type = DialogActivityType.Values.TransmissionOpened,
                    TransmissionId = Guid.Parse("019c27dc-76d0-7269-a8e1-b30fc5a53aec"),
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorName = null,
                        ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                    },
                    Description = []
                }
            )
            .ExecuteAndAssert<Conflict>();
    }

    [Fact]
    public Task Can_Create_Activity_On_TransmissionId()
    {
        var transmissionId = Guid.Parse("019c0f25-9759-70c5-8d9d-f03f336a0b6f");
        return FlowBuilder.For(Application)
            .CreateComplexDialog((x, _) => x.Dto.Transmissions =
            [
                new TransmissionDto
                {
                    Id = transmissionId,
                    CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                    AuthorizationAttribute = null,
                    ExtendedType = null,
                    ExternalReference = null,
                    RelatedTransmissionId = null,
                    Type = DialogTransmissionType.Values.Submission,
                    Sender = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorName = null,
                        ActorId = "urn:altinn:person:legacy-selfidentified:Per"
                    },
                    Content = new TransmissionContentDto
                    {
                        Title = new ContentValueDto
                        {
                            Value =
                            [
                                new LocalizationDto
                                {
                                    Value = "title",
                                    LanguageCode = "nb"
                                }
                            ],
                            MediaType = MediaTypes.PlainText
                        },
                        Summary = null,
                        ContentReference = null
                    },
                    Attachments = [],
                    NavigationalActions = []
                }
            ])
            .AssertResult<CreateDialogSuccess>()
            .CreateActivity(
                (c, _) => c.Activity = new CreateActivityDto
                {
                    Id = Guid.Parse("019c27e4-b9be-7f48-b652-fa7aa5df094a"),
                    CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                    ExtendedType = null,
                    Type = DialogActivityType.Values.TransmissionOpened,
                    TransmissionId = transmissionId,
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorName = null,
                        ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                    },
                    Description = []
                }
            )
            .ExecuteAndAssert<CreateActivitySuccess>();
    }

    [Theory, ClassData(typeof(CreateInvalidActivityTestData))]
    public Task Validation_error(string _, Func<FlowContext, CreateActivityCommand> activity,
        List<string> errors)
    {
        return FlowBuilder.For(Application)
            .OverrideUtc(CreateInvalidActivityTestData.Time)
            .CreateSimpleDialog()
            .SendCommand(activity)
            .ExecuteAndAssert<ValidationError>(validationError =>
            {
                validationError.Errors.Select(e => e.ErrorMessage).Should().BeEquivalentTo(errors);
            });
    }

    private sealed class
        CreateInvalidActivityTestData : TheoryData<string, Func<FlowContext, CreateActivityCommand>,
        List<string>>
    {
        public static readonly DateTimeOffset Time = new(2001, 1, 1, 1, 1, 0, TimeSpan.Zero);

        public CreateInvalidActivityTestData()
        {
            Add("Empty Guid",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Empty,
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00c717c3-cd80-7cc7-9507-aa6a9fdd595b"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                ["'DialogId' must not be empty."]);

            Add("Activity.Id must not be Guid v7",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("019c27e4-b9be-7f48-b652-fa7aa5df094a"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("7c597b1a-c7f4-4c91-a13b-3b6de0ba59ef"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "Invalid Id. Expected big endian UUIDv7 format. Got '7c597b1a-c7f4-4c91-a13b-3b6de0ba59ef'.",
                    "Invalid Id. Expected the unix timestamp portion of the UUIDv7 to be in the past. Extrapolated '6302-08-09T22:01:26.2600000+00:00' from '7c597b1a-c7f4-4c91-a13b-3b6de0ba59ef'."
                ]);

            Add("Activity.Id must not be Guid v4",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("019c27e4-b9be-7f48-b652-fa7aa5df094a"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("7c597b1a-c7f4-4c91-a13b-3b6de0ba59ef"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "Invalid Id. Expected big endian UUIDv7 format. Got '7c597b1a-c7f4-4c91-a13b-3b6de0ba59ef'.",
                    "Invalid Id. Expected the unix timestamp portion of the UUIDv7 to be in the past. Extrapolated '6302-08-09T22:01:26.2600000+00:00' from '7c597b1a-c7f4-4c91-a13b-3b6de0ba59ef'.",
                ]);

            Add("Activity.Id must not be empty",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("019c27e4-b9be-7f48-b652-fa7aa5df094a"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Empty,
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "Invalid Id. Expected big endian UUIDv7 format. Got '00000000-0000-0000-0000-000000000000'."
                ]);

            Add("Activity.CreatedAt cant be in the future with a tolerance of 15 seconds",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7033-a7ed-848d8fa2578c"),
                        CreatedAt = Time.AddSeconds(16),
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "'CreatedAt' must be in the past.",
                ]);

            Add("Activity.ExtendedType must be a well formatted uri",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7033-a7ed-848d8fa2578c"),
                        CreatedAt = Time.AddSeconds(15),
                        ExtendedType = new Uri("http://example.com/%zz"),
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "'ExtendedType' is not a well formatted URI.",
                ]);

            Add("Activity.ExtendedType uri cant be 1025 or longer",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7033-a7ed-848d8fa2578c"),
                        CreatedAt = Time.AddSeconds(15),
                        ExtendedType = new Uri("http://localhost?q=" + new string('a', 1024 - 19)),
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "The length of 'ExtendedType' must be 1023 characters or fewer. You entered 1025 characters.",
                ]);

            Add("Activity.PerformedBy cant be null",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7033-a7ed-848d8fa2578c"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = null!,
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "'PerformedBy' must not be empty.",
                ]);

            Add("Activity.PerformedBy.ActorId must be a valid urn",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7f99-9357-4211b7d182ed"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "invalid-urn"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "'ActorId' must be on format 'urn:altinn:organization:identifier-no:{norwegian org-nr}', 'urn:altinn:person:identifier-no:{norwegian f-nr/d-nr}', 'urn:altinn:person:legacy-selfidentified:{username}' or 'urn:altinn:person:idporten-email:{e-mail}' with valid values, respectively.",
                ]);

            Add("Activity.PerformedBy cant have both ActorName and ActorId when ActorType is not ServiceOwner",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7f99-9357-4211b7d182ed"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = "cant have both name and id",
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "If 'ActorType' is 'ServiceOwner', both 'ActorId' and 'ActorName' must be null. For any other value of 'ActorType', 'ActorId' or 'ActorName' must be set, but not both simultaneously.",
                ]);

            Add("Activity.PerformedBy must have either actorName or ActorId when ActorType is snot ServiceOwner",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7868-b434-9bcd2891c588"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = null
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "If 'ActorType' is 'ServiceOwner', both 'ActorId' and 'ActorName' must be null. For any other value of 'ActorType', 'ActorId' or 'ActorName' must be set, but not both simultaneously.",
                ]);

            Add("Activity.Description is required when the type is 'Information'.",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7033-a7ed-848d8fa2578c"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.Information,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "Description is required when the type is 'Information'.",
                ]);

            Add("Activity.Description is only allowed when the type is 'Information'.",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7033-a7ed-848d8fa2578c"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description =
                        [
                            new LocalizationDto
                            {
                                Value = "value",
                                LanguageCode = "nb"
                            }
                        ]
                    },
                    IsSilentUpdate = false
                },
                [
                    "Description is only allowed when the type is 'Information'.",
                ]);

            Add("Activity.TransmissionId can only be used when ActivityType is TransmissionOpened",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7033-a7ed-848d8fa2578c"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.FormSubmitted,
                        TransmissionId = Guid.Parse("00bfa652-d580-7161-864d-09ef6558c7d8"),
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "Only activities of type TransmissionOpened can reference a transmission.",
                ]);

            Add("Activity.TransmissionId must be set then TransmissionOpened",
                ctx => new CreateActivityCommand
                {
                    DialogId = Guid.Parse("00bfa652-d580-7384-96c4-8d1a97e7118d"),
                    IfMatchDialogRevision = null,
                    Activity = new CreateActivityDto
                    {
                        Id = Guid.Parse("00bfa652-d580-7033-a7ed-848d8fa2578c"),
                        CreatedAt = Time,
                        ExtendedType = null,
                        Type = DialogActivityType.Values.TransmissionOpened,
                        TransmissionId = null,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = null,
                            ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
                        },
                        Description = []
                    },
                    IsSilentUpdate = false
                },
                [
                    "An activity of type TransmissionOpened needs to reference a transmission."
                ]);
        }
    }
}
