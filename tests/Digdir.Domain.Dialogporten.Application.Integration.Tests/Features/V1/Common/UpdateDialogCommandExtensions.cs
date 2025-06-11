using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Tool.Dialogporten.GenerateFakeData;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;

internal static class UpdateDialogCommandExtensions
{
    public static UpdateDialogCommand AddTransmission(this UpdateDialogCommand command, Action<TransmissionDto>? modify = null)
    {
        var transmission = new TransmissionDto
        {
            Type = DialogTransmissionType.Values.Information,
            Content = new()
            {
                Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) },
                Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) }
            },
            Sender = new() { ActorType = ActorType.Values.ServiceOwner }
        };

        modify?.Invoke(transmission);
        command.Dto.Transmissions.Add(transmission);
        return command;
    }

    public static UpdateDialogCommand AddGuiAction(this UpdateDialogCommand command, Action<GuiActionDto>? modify = null)
    {
        var guiAction = new GuiActionDto
        {
            Action = "Test action",
            Title = [new() { LanguageCode = "nb", Value = "Test action" }],
            Priority = DialogGuiActionPriority.Values.Tertiary,
            Url = new Uri("https://example.com"),
        };

        modify?.Invoke(guiAction);
        command.Dto.GuiActions.Add(guiAction);
        return command;
    }

    public static UpdateDialogCommand AddApiAction(this UpdateDialogCommand command, Action<ApiActionDto>? modify = null)
    {
        var apiAction = new ApiActionDto
        {
            Action = "Test action",
            Name = "Test action",
            Endpoints = [new() { Url = new Uri("https://example.com"), HttpMethod = HttpVerb.Values.GET }]
        };

        modify?.Invoke(apiAction);
        command.Dto.ApiActions.Add(apiAction);
        return command;
    }

    public static UpdateDialogCommand AddAttachment(this UpdateDialogCommand command, Action<AttachmentDto>? modify = null)
    {
        var attachment = new AttachmentDto
        {
            DisplayName = DialogGenerator.GenerateFakeLocalizations(3),
            Urls =
            [
                new()
                {
                    Url = new Uri("https://example.com"),
                    ConsumerType = AttachmentUrlConsumerType.Values.Gui
                }
            ]
        };

        modify?.Invoke(attachment);
        command.Dto.Attachments.Add(attachment);
        return command;
    }

    public static UpdateDialogCommand ChangeTitle(this UpdateDialogCommand command, Action<ContentValueDto>? modify = null)
    {
        var contentDto = new ContentValueDto
        {
            Value = DialogGenerator.GenerateFakeLocalizations(3)
        };

        modify?.Invoke(contentDto);
        command.Dto.Content!.Title = contentDto;
        return command;
    }

    public static UpdateDialogCommand AddActivity(this UpdateDialogCommand command, Action<ActivityDto>? modify = null)
    {
        var activity = new ActivityDto
        {
            Type = DialogActivityType.Values.Information,
            Description = DialogGenerator.GenerateFakeLocalizations(3),
            PerformedBy = new() { ActorType = ActorType.Values.ServiceOwner }
        };

        modify?.Invoke(activity);
        command.Dto.Activities.Add(activity);
        return command;
    }
}
