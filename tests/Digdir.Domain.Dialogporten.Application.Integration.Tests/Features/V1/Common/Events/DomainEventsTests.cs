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
using MassTransit.Internals;
using MassTransit.Testing;

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
            act.Should().NotThrow($"all domain events must have a mapping in {nameof(CloudEventTypes)} ({domainEventType.Name} is missing)");
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
            act.Should().NotThrow($"all activity types must have a mapping in {nameof(CloudEventTypes)} ({activityType} is missing)");
        });
    }

    [Fact]
    public async Task Creates_CloudEvents_When_Dialog_Created()
    {
        // Arrange
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();

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
            attachments: DialogGenerator.GenerateFakeDialogAttachments(3));
        var dto = createDialogCommand.Dto;
        dto.Transmissions.Add(transmission);

        // Act
        await Application.Send(createDialogCommand);

        await harness.Consumed
            .SelectAsync<DialogCreatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        await harness.Consumed
            .SelectAsync<DialogActivityCreatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .Take(activities.Count)
            .ToListAsync();

        await harness.Consumed
            .SelectAsync<DialogTransmissionCreatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        var cloudEvents = Application.PopPublishedCloudEvents();

        // Assert
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.ResourceInstance == dto.Id.ToString());
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Resource == dto.ServiceResource);
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Subject == dto.Party);

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogCreatedDomainEvent)));

        allActivityTypes.ForEach(activityType =>
            cloudEvents.Should().ContainSingle(cloudEvent =>
                cloudEvent.Type == CloudEventTypes.Get(activityType.ToString())));

        cloudEvents.Count(cloudEvent => cloudEvent.Type == CloudEventTypes.Get(nameof(DialogTransmissionCreatedDomainEvent)))
            .Should().Be(dto.Transmissions.Count);

        cloudEvents.Count
            .Should()
            // +1 for the dialog created event
            .Be(dto.Activities.Count + dto.Transmissions.Count + 1);
    }

    [Fact]
    public async Task Creates_CloudEvent_When_Dialog_Updates()
    {
        // Arrange
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(
            activities: [],
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
        await harness.Consumed
            .SelectAsync<DialogUpdatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();
        var cloudEvents = Application.PopPublishedCloudEvents();

        // Assert
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.ResourceInstance == dto.Id!.Value.ToString());
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Resource == dto.ServiceResource);
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Subject == dto.Party);

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogUpdatedDomainEvent)));
    }

    [Fact]
    public async Task Creates_Update_Event_And_Activity_Created_Event_When_Activity_Is_Added()
    {
        // Arrange
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();
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

        await harness.Consumed
            .SelectAsync<DialogUpdatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        await harness.Consumed
            .SelectAsync<DialogActivityCreatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();
        var cloudEvents = Application.PopPublishedCloudEvents();

        // Assert
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.ResourceInstance == dto.Id!.Value.ToString());
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Resource == dto.ServiceResource);
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Subject == dto.Party);

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogUpdatedDomainEvent)));

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogActivityType.Values.DialogClosed)));
    }

    [Fact]
    public async Task Creates_Update_Event_And_Transmission_Created_Event_When_Transmission_Is_Added()
    {
        // Arrange
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();
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

        await harness.Consumed
            .SelectAsync<DialogUpdatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        await harness.Consumed
            .SelectAsync<DialogTransmissionCreatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        var cloudEvents = Application.PopPublishedCloudEvents();

        // Assert
        cloudEvents.Should().HaveCount(3); // DialogUpdated, TransmissionCreated, DialogCreated
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.ResourceInstance == dto.Id!.Value.ToString());
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Resource == dto.ServiceResource);
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Subject == dto.Party);

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogUpdatedDomainEvent)));

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogTransmissionCreatedDomainEvent)));
    }

    [Fact]
    public async Task Creates_CloudEvent_When_Attachments_Updates()
    {
        // Arrange
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(
            id: dialogId,
            attachments: []);

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
        await harness.Consumed
            .SelectAsync<DialogUpdatedDomainEvent>(x => x.Context.Message.DialogId == dialogId)
            .FirstOrDefault();
        var cloudEvents = Application.PopPublishedCloudEvents();

        // Assert
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.ResourceInstance == dialogId.ToString());
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Resource == createDialogCommand.Dto.ServiceResource);
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Subject == createDialogCommand.Dto.Party);

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogUpdatedDomainEvent)));
    }
    [Fact]
    public async Task Creates_CloudEvents_When_Dialog_Deleted()
    {
        // Arrange
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(id: dialogId, attachments: [], activities: []);

        await Application.Send(createDialogCommand);

        // Act
        var deleteDialogCommand = new DeleteDialogCommand
        {
            Id = dialogId
        };
        await Application.Send(deleteDialogCommand);
        await harness.Consumed
            .SelectAsync<DialogDeletedDomainEvent>(x => x.Context.Message.DialogId == dialogId)
            .FirstOrDefault();
        var cloudEvents = Application.PopPublishedCloudEvents();

        // Assert
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.ResourceInstance == dialogId.ToString());
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Resource == createDialogCommand.Dto.ServiceResource);
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Subject == createDialogCommand.Dto.Party);

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogDeletedDomainEvent)));
    }

    [Fact]
    public async Task Creates_DialogDeletedEvent_When_Dialog_Purged()
    {
        // Arrange
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(id: dialogId, attachments: [], activities: []);

        await Application.Send(createDialogCommand);

        // Act
        var purgeCommand = new PurgeDialogCommand
        {
            DialogId = dialogId
        };

        await Application.Send(purgeCommand);
        await harness.Consumed
            .SelectAsync<DialogDeletedDomainEvent>(x => x.Context.Message.DialogId == dialogId)
            .FirstOrDefault();
        var cloudEvents = Application.PopPublishedCloudEvents();

        // Assert
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.ResourceInstance == dialogId.ToString());
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Resource == createDialogCommand.Dto.ServiceResource);
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Subject == createDialogCommand.Dto.Party);

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogDeletedDomainEvent)));
    }

    [Fact]
    public async Task Creates_CloudEvent_When_Dialog_Is_Restored()
    {
        // Arrange
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();
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

        await harness.Consumed
            .SelectAsync<DialogRestoredDomainEvent>(x => x.Context.Message.DialogId == dialogId)
            .FirstOrDefault();

        var cloudEvents = Application.PopPublishedCloudEvents();

        // Assert
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.ResourceInstance == dialogId.ToString());
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Resource == createDialogCommand.Dto.ServiceResource);
        cloudEvents.Should().OnlyContain(cloudEvent => cloudEvent.Subject == createDialogCommand.Dto.Party);

        cloudEvents.Should().ContainSingle(cloudEvent =>
            cloudEvent.Type == CloudEventTypes.Get(nameof(DialogRestoredDomainEvent)));
    }

    [Fact]
    public async Task AltinnEvents_Should_Be_Disabled_When_DisableAltinnEvents_Is_Set()
    {
        // Arrange - Create
        var harness = await Application.ConfigureServicesWithMassTransitTestHarness();

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
        await harness.Consumed
            .SelectAsync<DialogCreatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();
        await harness.Consumed
            .SelectAsync<DialogActivityCreatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

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
        await harness.Consumed
            .SelectAsync<DialogUpdatedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        // Arrange - Delete
        var deleteDialogCommand = new DeleteDialogCommand
        {
            Id = dto.Id!.Value,
            DisableAltinnEvents = true
        };

        // Act - Delete
        await Application.Send(deleteDialogCommand);
        await harness.Consumed
            .SelectAsync<DialogDeletedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        // Arrange - Restore
        var restoreDialogCommand = new RestoreDialogCommand
        {
            DialogId = dto.Id!.Value,
            DisableAltinnEvents = true
        };

        // Act - Restore
        await Application.Send(restoreDialogCommand);
        await harness.Consumed
            .SelectAsync<DialogRestoredDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        // Arrange - Purge
        var purgeCommand = new PurgeDialogCommand
        {
            DialogId = dto.Id!.Value,
            DisableAltinnEvents = true
        };

        // Act - Purge
        await Application.Send(purgeCommand);
        await harness.Consumed
            .SelectAsync<DialogDeletedDomainEvent>(x => x.Context.Message.DialogId == dto.Id)
            .FirstOrDefault();

        // Assert
        var cloudEvents = Application.PopPublishedCloudEvents();
        cloudEvents.Should().BeEmpty();
    }
}
