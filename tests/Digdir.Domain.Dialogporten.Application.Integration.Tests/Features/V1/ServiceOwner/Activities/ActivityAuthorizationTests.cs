using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Activities;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ActivityAuthorizationTests : ApplicationCollectionFixture
{
    public ActivityAuthorizationTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Cannot_Create_Correspondence_Activities_Without_Required_Scope() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Activities
                    .Add(DialogGenerator.GenerateFakeDialogActivity(
                        type: DialogActivityType.Values.CorrespondenceOpened));
                x.Dto.Activities
                    .Add(DialogGenerator.GenerateFakeDialogActivity(
                        type: DialogActivityType.Values.CorrespondenceConfirmed));
            })
            .ExecuteAndAssert<Forbidden>(x =>
            {
                x.Reasons.Should().ContainSingle(x => x.Contains(AuthorizationScope.CorrespondenceScope));
                x.Reasons.Should().ContainSingle(x => x.Contains(nameof(DialogActivityType.Values.CorrespondenceOpened)));
            });

    [Fact]
    public Task Can_Create_Correspondence_Activities_With_Required_Scope() =>
        FlowBuilder.For(Application)
            .AsCorrespondenceUser()
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Activities
                    .Add(DialogGenerator.GenerateFakeDialogActivity(
                        type: DialogActivityType.Values.CorrespondenceOpened));
                x.Dto.Activities
                    .Add(DialogGenerator.GenerateFakeDialogActivity(
                        type: DialogActivityType.Values.CorrespondenceConfirmed));
            })
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Fact]
    public Task Cannot_Update_Correspondence_Activities_Without_Required_Scope() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.Dto.Activities.Add(new()
                {
                    Type = DialogActivityType.Values.CorrespondenceOpened,
                    PerformedBy = new ActorDto { ActorType = ActorType.Values.ServiceOwner }
                });

                x.Dto.Activities.Add(new()
                {
                    Type = DialogActivityType.Values.CorrespondenceConfirmed,
                    PerformedBy = new ActorDto { ActorType = ActorType.Values.ServiceOwner }
                });
            })
            .ExecuteAndAssert<Forbidden>(x =>
            {
                x.Reasons.Should().ContainSingle(x => x.Contains(AuthorizationScope.CorrespondenceScope));
                x.Reasons.Should().ContainSingle(x => x.Contains(nameof(DialogActivityType.Values.CorrespondenceOpened)));
            });

    [Fact]
    public Task Can_Update_Correspondence_Activities_With_Required_Scope() =>
        FlowBuilder.For(Application)
            .AsCorrespondenceUser()
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.Dto.Activities.Add(new()
                {
                    Type = DialogActivityType.Values.CorrespondenceOpened,
                    PerformedBy = new ActorDto { ActorType = ActorType.Values.ServiceOwner }
                });

                x.Dto.Activities.Add(new()
                {
                    Type = DialogActivityType.Values.CorrespondenceConfirmed,
                    PerformedBy = new ActorDto { ActorType = ActorType.Values.ServiceOwner }
                });
            })
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public Task Can_Update_NonCorrespondence_Activity_Without_Scope_When_Dialog_Already_Has_Correspondence_Activity() =>
        FlowBuilder.For(Application)
            .AsCorrespondenceUser()
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Activities
                    .Add(DialogGenerator.GenerateFakeDialogActivity(
                        type: DialogActivityType.Values.CorrespondenceOpened));
            })
            .AssertResult<CreateDialogSuccess>()
            .AsIntegrationTestUser()
            .UpdateDialog(x =>
            {
                x.Dto.Activities.Add(new()
                {
                    Type = DialogActivityType.Values.DialogCreated,
                    PerformedBy = new ActorDto { ActorType = ActorType.Values.ServiceOwner }
                });
            })
            .ExecuteAndAssert<UpdateDialogSuccess>();

    [Fact]
    public Task Cannot_Create_Correspondence_Activity_With_CreateActivity_Without_Required_Scope() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateActivity((command, _) => command.Activity = new CreateActivityDto
            {
                CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                Type = DialogActivityType.Values.CorrespondenceOpened,
                PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.ServiceOwner
                },
                Description = []
            })
            .ExecuteAndAssert<Forbidden>(x =>
            {
                x.Reasons.Should().ContainSingle(reason => reason.Contains(AuthorizationScope.CorrespondenceScope));
                x.Reasons.Should().ContainSingle(reason => reason.Contains(nameof(DialogActivityType.Values.CorrespondenceOpened)));
            });

    [Fact]
    public Task Can_Create_Correspondence_Activity_With_CreateActivity_With_Required_Scope() =>
        FlowBuilder.For(Application)
            .AsCorrespondenceUser()
            .CreateSimpleDialog()
            .CreateActivity((command, _) => command.Activity = new CreateActivityDto
            {
                CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                Type = DialogActivityType.Values.CorrespondenceOpened,
                PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.ServiceOwner
                },
                Description = []
            })
            .ExecuteAndAssert<CreateActivitySuccess>();

    [Fact]
    public Task Can_Create_NonCorrespondence_Activity_Without_Scope_When_Dialog_Already_Has_Correspondence_Activity() =>
        FlowBuilder.For(Application)
            .AsCorrespondenceUser()
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Activities
                    .Add(DialogGenerator.GenerateFakeDialogActivity(
                        type: DialogActivityType.Values.CorrespondenceOpened));
            })
            .AssertResult<CreateDialogSuccess>()
            .AsIntegrationTestUser()
            .CreateActivity((command, _) => command.Activity = new CreateActivityDto
            {
                CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                Type = DialogActivityType.Values.DialogCreated,
                PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.ServiceOwner
                },
                Description = []
            })
            .ExecuteAndAssert<CreateActivitySuccess>();
}
