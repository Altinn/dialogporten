using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Activities;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ActivityAuthorizationTests : ApplicationCollectionFixture
{
    public ActivityAuthorizationTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Cannot_Create_Correspondence_Activities_Without_Required_Scope() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
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
        FlowBuilder.For(Application, x =>
            {
                x.RemoveAll<IUser>();
                x.AddSingleton<IUser>(CreateUserWithScope(AuthorizationScope.CorrespondenceScope));
            })
            .CreateSimpleDialog(x =>
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
        FlowBuilder.For(Application, x =>
            {
                x.RemoveAll<IUser>();
                x.AddSingleton<IUser>(CreateUserWithScope(AuthorizationScope.CorrespondenceScope));
            })
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

    private static IntegrationTestUser CreateUserWithScope(string scope) => new([new("scope", scope)]);
}
