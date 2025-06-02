using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using FluentAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Restore;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events.Activities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneOf.Types;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;

using AttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.AttachmentDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Events;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DomainEventsTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public void All_DomainEvents_Must_Have_A_Mapping_In_CloudEventTypes()
    {
        var domainEventTypes = typeof(IDomainEvent).Assembly.GetTypes()
            .Where(type => typeof(IDomainEvent).IsAssignableFrom(type)
                           && !type.IsInterface
                           // DialogActivityCreatedDomainEvent maps based on activity type
                           // See All_DialogActivityTypes_Must_Have_A_Mapping_In_CloudEventTypes
                           && type != typeof(DialogActivityCreatedDomainEvent)
                           && type != typeof(DomainEvent))
            .ToList();

        // Act/Assert
        domainEventTypes.ForEach(domainEventType =>
        {
            Action act = () => CloudEventTypes.Get(domainEventType.Name);
            act.Should()
                .NotThrow(
                    $"all domain events must have a mapping in {nameof(CloudEventTypes)} ({domainEventType.Name} is missing)");
        });
    }

    [Fact]
    public void All_DialogActivityTypes_Must_Have_A_Mapping_In_CloudEventTypes()
    {
        // Arrange
        var allActivityTypes = Enum.GetValues<DialogActivityType.Values>().ToList();

        // Act/Assert
        allActivityTypes.ForEach(activityType =>
        {
            Action act = () => CloudEventTypes.Get(activityType.ToString());
            act.Should()
                .NotThrow(
                    $"all activity types must have a mapping in {nameof(CloudEventTypes)} ({activityType} is missing)");
        });
    }

    [Fact]
    public async Task Creates_DomainEvents_When_Dialog_Created()
    {
        var activityCount = 0;

        await FlowBuilder.For(Application, x =>
            {
                x.RemoveAll<IUser>();
                x.AddSingleton<IUser>(CreateUserWithScope(AuthorizationScope.CorrespondenceScope));
            })
            .CreateSimpleDialog(x =>
            {
                var allActivityTypes = Enum.GetValues<DialogActivityType.Values>().ToList();

                x.Dto.Activities = allActivityTypes
                    .Select(activityType => DialogGenerator.GenerateFakeDialogActivity(activityType))
                    .ToList();

                activityCount = x.Dto.Activities.Count;

                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1).First();
                x.Dto.Activities
                    .Single(x => x.Type == DialogActivityType.Values.TransmissionOpened)
                    .TransmissionId = transmission.Id;

                x.Dto.Transmissions = [transmission];
            })
            .ExecuteAndAssert<CreateDialogSuccess>(x =>
            {
                var domainEvents = Application.GetPublishedEvents();
                domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogTransmissionCreatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogActivityCreatedDomainEvent>().Should().HaveCount(activityCount);
            });
    }

    [Fact]
    public Task Creates_CloudEvent_When_Dialog_Updates() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x => x.Dto.ExtendedStatus = "Updated status")
            .ExecuteAndAssert<UpdateDialogSuccess>(x =>
            {
                var domainEvents = Application.GetPublishedEvents();
                domainEvents.OfType<DialogUpdatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);
            });

    [Fact]
    public Task Creates_Update_Event_And_Activity_Created_Event_When_Activity_Is_Added() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
            {
                x.Dto.Activities.Add(new()
                {
                    Type = DialogActivityType.Values.DialogOpened,
                    PerformedBy = new() { ActorType = ActorType.Values.ServiceOwner }
                });
            })
            .ExecuteAndAssert<UpdateDialogSuccess>(_ =>
            {
                var domainEvents = Application.GetPublishedEvents();
                domainEvents.OfType<DialogUpdatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogActivityCreatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

                domainEvents.Count.Should().Be(3);
            });

    [Fact]
    public Task Creates_Update_Event_And_Transmission_Created_Event_When_Transmission_Is_Added() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
            {
                x.Dto.Transmissions.Add(new()
                {
                    Type = DialogTransmissionType.Values.Information,
                    Sender = new() { ActorType = ActorType.Values.ServiceOwner },
                    Content = new()
                    {
                        Title = new() { Value = [new() { LanguageCode = "nb", Value = "Title" }] },
                        Summary = new() { Value = [new() { LanguageCode = "nb", Value = "Summary" }] }
                    }
                });
            })
            .ExecuteAndAssert<UpdateDialogSuccess>(_ =>
            {
                var domainEvents = Application.GetPublishedEvents();
                domainEvents.OfType<DialogUpdatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogTransmissionCreatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

                domainEvents.Count.Should().Be(3);
            });

    [Fact]
    public Task Creates_CloudEvent_When_Attachments_Updates() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
            {
                x.Dto.Attachments = [new AttachmentDto
                {
                    DisplayName = DialogGenerator.GenerateFakeLocalizations(3),
                    Urls = [new()
                    {
                        ConsumerType = AttachmentUrlConsumerType.Values.Gui,
                        Url = new Uri("https://example.com")
                    }]
                }];
            })
            .ExecuteAndAssert<UpdateDialogSuccess>(x =>
            {
                var domainEvents = Application.GetPublishedEvents();
                domainEvents.OfType<DialogUpdatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

                domainEvents.Count.Should().Be(2);
            });

    [Fact]
    public Task Creates_CloudEvents_When_Dialog_Deleted() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .ExecuteAndAssert<DeleteDialogSuccess>(x =>
            {
                var domainEvents = Application.GetPublishedEvents();
                domainEvents.OfType<DialogDeletedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

                domainEvents.Count.Should().Be(2);
            });

    [Fact]
    public Task Creates_DialogDeletedEvent_When_Dialog_Purged() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .PurgeDialog()
            .ExecuteAndAssert<Success>(x =>
            {
                var domainEvents = Application.GetPublishedEvents();
                domainEvents.OfType<DialogDeletedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

                domainEvents.Count.Should().Be(2);
            });

    [Fact]
    public Task Creates_CloudEvent_When_Dialog_Is_Restored() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .RestoreDialog()
            .ExecuteAndAssert<RestoreDialogSuccess>(x =>
            {
                var domainEvents = Application.GetPublishedEvents();
                domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogDeletedDomainEvent>().Should().HaveCount(1);
                domainEvents.OfType<DialogRestoredDomainEvent>().Should().HaveCount(1);

                domainEvents.Count.Should().Be(3);
            });

    [Fact]
    public Task AltinnEvents_Should_Be_Disabled_When_IsSilentUpdate_Is_Set() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Activities.Add(DialogGenerator.GenerateFakeDialogActivity(DialogActivityType.Values.Information));
                x.IsSilentUpdate = true;
            })
            .UpdateDialog(x =>
            {
                x.IsSilentUpdate = true;
                x.Dto.ExtendedStatus = "Updated status";
            })
            .AssertResult<UpdateDialogSuccess>()
            .SendCommand((_, ctx) => new DeleteDialogCommand
            {
                Id = ctx.GetDialogId(),
                IsSilentUpdate = true
            })
            .RestoreDialog(x => x.IsSilentUpdate = true)
            .SendCommand((_, ctx) => new PurgeDialogCommand
            {
                IsSilentUpdate = true,
                DialogId = ctx.GetDialogId()
            })
            .ExecuteAndAssert<Success>(_ =>
            {
                var publishedEvents = Application.GetPublishedEvents();

                publishedEvents
                    .OfType<IDomainEvent>()
                    .Should()
                    .NotBeEmpty();

                publishedEvents
                    .OfType<IDomainEvent>()
                    .All(e => e.Metadata[Constants.IsSilentUpdate] == bool.TrueString)
                    .Should()
                    .BeTrue();
            });

    private static IntegrationTestUser CreateUserWithScope(string scope) => new([new("scope", scope)]);
}
