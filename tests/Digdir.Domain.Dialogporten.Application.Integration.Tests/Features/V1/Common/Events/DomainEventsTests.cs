using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AutoMapper;
using FluentAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Restore;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events.Activities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Events;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DomainEventsTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private static readonly IMapper Mapper;

    static DomainEventsTests()
    {
        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(ApplicationExtensions).Assembly);
        });

        Mapper = mapperConfiguration.CreateMapper();
    }

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
        // Arrange
        var allActivityTypes = Enum.GetValues<DialogActivityType.Values>().ToList();
        var activities = allActivityTypes
            .Select(activityType => DialogGenerator.GenerateFakeDialogActivity(activityType))
            .ToList();

        var transmissionOpenedActivity = activities
            .Single(x => x.Type == DialogActivityType.Values.TransmissionOpened);
        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1).First();
        transmissionOpenedActivity.TransmissionId = transmission.Id;

        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(
            activities: activities,
            transmissions: [transmission],
            attachments: DialogGenerator.GenerateFakeDialogAttachments(3));

        // Act
        await Application.Send(createDialogCommand);

        var domainEvents = Application.GetPublishedEvents();

        // Assert
        domainEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);
        domainEvents.OfType<DialogTransmissionCreatedDomainEvent>().Should().HaveCount(1);
        domainEvents.OfType<DialogActivityCreatedDomainEvent>().Should().HaveCount(activities.Count);

        domainEvents.Count
            .Should()
            // +1 for the dialog created event
            .Be(createDialogCommand.Dto.Activities.Count +
                createDialogCommand.Dto.Transmissions.Count + 1);
    }

    [Fact]
    public async Task Creates_CloudEvent_When_Dialog_Updates()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(
            activities: [],
            transmissions: [],
            progress: 0,
            attachments: []);

        await Application.Send(createDialogCommand);
        var dto = createDialogCommand.Dto;

        var getDialogResult = await Application.Send(new GetDialogQuery { DialogId = dto.Id!.Value });
        getDialogResult.TryPickT0(out var getDialogDto, out _);

        var updateDialogDto = Mapper.Map<UpdateDialogDto>(getDialogDto);

        // Act
        updateDialogDto.Progress = 1;

        var updateDialogCommand = new UpdateDialogCommand
        {
            Id = dto.Id!.Value,
            Dto = updateDialogDto
        };

        await Application.Send(updateDialogCommand);

        var publishedEvents = Application.GetPublishedEvents();

        // Assert
        publishedEvents.OfType<DialogUpdatedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

        // Created + Updated
        publishedEvents.Count.Should().Be(2);
    }

    [Fact]
    public async Task Creates_Update_Event_And_Activity_Created_Event_When_Activity_Is_Added()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();

        await Application.Send(createDialogCommand);
        var dto = createDialogCommand.Dto;

        var getDialogResult = await Application.Send(new GetDialogQuery { DialogId = dto.Id!.Value });
        getDialogResult.TryPickT0(out var getDialogDto, out _);

        var updateDialogDto = Mapper.Map<UpdateDialogDto>(getDialogDto);

        // Act
        updateDialogDto.Activities = [new ActivityDto
        {
            Type = DialogActivityType.Values.DialogClosed,
            PerformedBy = new() {ActorType = ActorType.Values.ServiceOwner}
        }];

        var updateDialogCommand = new UpdateDialogCommand
        {
            Id = dto.Id!.Value,
            Dto = updateDialogDto
        };

        await Application.Send(updateDialogCommand);

        var publishedEvents = Application.GetPublishedEvents();

        // Assert
        publishedEvents.OfType<DialogUpdatedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogActivityCreatedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

        // Created + Updated + ActivityCreated
        publishedEvents.Count.Should().Be(3);
    }

    [Fact]
    public async Task Creates_Update_Event_And_Transmission_Created_Event_When_Transmission_Is_Added()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();

        await Application.Send(createDialogCommand);
        var dto = createDialogCommand.Dto;

        var getDialogResult = await Application.Send(new GetDialogQuery { DialogId = dto.Id!.Value });
        getDialogResult.TryPickT0(out var getDialogDto, out _);

        var updateDialogDto = Mapper.Map<UpdateDialogDto>(getDialogDto);

        updateDialogDto.Transmissions = [new()
        {
            Type = DialogTransmissionType.Values.Information,
            Sender = new() { ActorType = ActorType.Values.ServiceOwner },
            Content = new()
            {
                Title = new() { Value = [new() {  LanguageCode = "nb", Value ="Title" }] },
                Summary = new() { Value = [new() { LanguageCode = "nb", Value = "Summary" }] }
            }
        }];

        var updateDialogCommand = new UpdateDialogCommand
        {
            Id = dto.Id!.Value,
            Dto = updateDialogDto
        };

        // Act
        await Application.Send(updateDialogCommand);

        var publishedEvents = Application.GetPublishedEvents();

        // Assert
        publishedEvents.OfType<DialogUpdatedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogTransmissionCreatedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

        // Created + Updated + TransmissionCreated
        publishedEvents.Count.Should().Be(3);
    }

    [Fact]
    public async Task Creates_CloudEvent_When_Attachments_Updates()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(dialogId);

        await Application.Send(createDialogCommand);

        var getDialogResult = await Application.Send(new GetDialogQuery { DialogId = dialogId });
        getDialogResult.TryPickT0(out var getDialogDto, out _);

        var updateDialogDto = Mapper.Map<UpdateDialogDto>(getDialogDto);

        // Act
        updateDialogDto.Attachments = [new AttachmentDto
        {
            DisplayName = DialogGenerator.GenerateFakeLocalizations(3),
            Urls = [new()
            {
                ConsumerType = AttachmentUrlConsumerType.Values.Gui,
                Url = new Uri("https://example.com")
            }]
        }];

        var updateDialogCommand = new UpdateDialogCommand
        {
            Id = dialogId,
            Dto = updateDialogDto
        };

        await Application.Send(updateDialogCommand);

        var publishedEvents = Application.GetPublishedEvents();

        // Assert
        publishedEvents.OfType<DialogUpdatedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

        // Created + Updated
        publishedEvents.Count.Should().Be(2);
    }

    [Fact]
    public async Task Creates_CloudEvents_When_Dialog_Deleted()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(dialogId);

        await Application.Send(createDialogCommand);

        // Act
        var deleteDialogCommand = new DeleteDialogCommand
        {
            Id = dialogId
        };
        await Application.Send(deleteDialogCommand);

        var publishedEvents = Application.GetPublishedEvents();

        // Assert
        publishedEvents.OfType<DialogDeletedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

        // Created + Deleted
        publishedEvents.Count.Should().Be(2);
    }

    [Fact]
    public async Task Creates_DialogDeletedEvent_When_Dialog_Purged()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(dialogId);

        await Application.Send(createDialogCommand);

        // Act
        var purgeCommand = new PurgeDialogCommand
        {
            DialogId = dialogId
        };

        await Application.Send(purgeCommand);

        var publishedEvents = Application.GetPublishedEvents();

        // Assert
        publishedEvents.OfType<DialogDeletedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);

        // Created + Deleted
        publishedEvents.Count.Should().Be(2);
    }

    [Fact]
    public async Task Creates_CloudEvent_When_Dialog_Is_Restored()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(dialogId);

        await Application.Send(createDialogCommand);

        var deleteDialogCommand = new DeleteDialogCommand
        {
            Id = dialogId
        };

        await Application.Send(deleteDialogCommand);

        // Act
        var restoreDialogCommand = new RestoreDialogCommand
        {
            DialogId = dialogId
        };

        await Application.Send(restoreDialogCommand);

        var publishedEvents = Application.GetPublishedEvents();

        // Assert
        publishedEvents.OfType<DialogCreatedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogDeletedDomainEvent>().Should().HaveCount(1);
        publishedEvents.OfType<DialogRestoredDomainEvent>().Should().HaveCount(1);

        // Created + Restored + Deleted
        publishedEvents.Count.Should().Be(3);
    }

    [Fact]
    public async Task AltinnEvents_Should_Be_Disabled_When_DisableAltinnEvents_Is_Set()
    {
        // Arrange - Create
        var activity = DialogGenerator.GenerateFakeDialogActivity(DialogActivityType.Values.Information);
        var initialProgress = 1;

        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(
            disableAltinnEvents: true,
            activities: [activity],
            attachments: [],
            progress: initialProgress);
        var dto = createDialogCommand.Dto;

        // Act - Create
        await Application.Send(createDialogCommand);

        // Arrange - Update
        var getDialogResult = await Application.Send(new GetDialogQuery { DialogId = dto.Id!.Value });
        getDialogResult.TryPickT0(out var getDialogDto, out _);

        var updateDialogDto = Mapper.Map<UpdateDialogDto>(getDialogDto);
        updateDialogDto.Progress = ++initialProgress;
        var updateDialogCommand = new UpdateDialogCommand
        {
            Id = dto.Id!.Value,
            Dto = updateDialogDto,
            DisableAltinnEvents = true
        };

        // Act - Update
        await Application.Send(updateDialogCommand);

        // Arrange - Delete
        var deleteDialogCommand = new DeleteDialogCommand
        {
            Id = dto.Id!.Value,
            DisableAltinnEvents = true
        };

        // Act - Delete
        await Application.Send(deleteDialogCommand);

        // Arrange - Restore
        var restoreDialogCommand = new RestoreDialogCommand
        {
            DialogId = dto.Id!.Value,
            DisableAltinnEvents = true
        };

        // Act - Restore
        await Application.Send(restoreDialogCommand);

        // Arrange - Purge
        var purgeCommand = new PurgeDialogCommand
        {
            DialogId = dto.Id!.Value,
            DisableAltinnEvents = true
        };

        // Act - Purge
        await Application.Send(purgeCommand);

        // Assert
        var publishedEvents = Application.GetPublishedEvents();

        publishedEvents
            .OfType<IDomainEvent>()
            .Should()
            .NotBeEmpty();

        publishedEvents
            .OfType<IDomainEvent>()
            .All(x => x.Metadata[Constants.DisableAltinnEvents] == bool.TrueString)
            .Should()
            .BeTrue();
    }
}
