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
using Digdir.Domain.Dialogporten.Domain.Attachments;
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

        cloudEvents.Count
            .Should()
            // +1 for the dialog created event
            .Be(dto.Activities.Count + 1);
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
}
