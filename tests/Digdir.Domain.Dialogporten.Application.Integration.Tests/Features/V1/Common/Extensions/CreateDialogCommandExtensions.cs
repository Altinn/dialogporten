using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Tool.Dialogporten.GenerateFakeData;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;

internal static class CreateDialogCommandExtensions
{
    public static CreateDialogCommand AddServiceOwnerLabels(this CreateDialogCommand command, params string[] labels)
    {
        command.Dto.ServiceOwnerContext ??= new DialogServiceOwnerContextDto();
        foreach (var label in labels)
        {
            command.Dto.ServiceOwnerContext.ServiceOwnerLabels.Add(new ServiceOwnerLabelDto { Value = label });
        }
        return command;
    }

    public static CreateDialogCommand AddTransmission(this CreateDialogCommand command, Action<TransmissionDto>? modify = null)
    {
        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
        modify?.Invoke(transmission);
        command.Dto.Transmissions.Add(transmission);
        return command;
    }

    public static CreateDialogCommand AddMainContentReference(
        this CreateDialogCommand command,
        Action<ContentValueDto>? modify = null)
    {
        var mainContentReference = new ContentValueDto
        {
            MediaType = MediaTypes.EmbeddableMarkdown,
            Value =
            [
                new LocalizationDto
                {
                    LanguageCode = "nb",
                    Value = "https://localhost/nb"
                },
                new LocalizationDto
                {
                    LanguageCode = "nn",
                    Value = "https://localhost/nn"
                },
                new LocalizationDto
                {
                    LanguageCode = "en",
                    Value = "https://localhost/en"
                }
            ]
        };
        modify?.Invoke(mainContentReference);
        command.Dto.Content!.MainContentReference = mainContentReference;
        return command;
    }

    public static CreateDialogCommand AddActivity(this CreateDialogCommand command, DialogActivityType.Values? type = null, Action<ActivityDto>? modify = null)
    {
        var activity = DialogGenerator.GenerateFakeDialogActivity(type);
        modify?.Invoke(activity);
        command.Dto.Activities.Add(activity);
        return command;
    }

    public static CreateDialogCommand AddAttachment(this CreateDialogCommand command, Action<AttachmentDto>? modify = null)
    {
        var attachment = DialogGenerator.GenerateFakeDialogAttachment();
        modify?.Invoke(attachment);
        command.Dto.Attachments.Add(attachment);
        return command;
    }

    public static CreateDialogCommand AddApiAction(this CreateDialogCommand command, Action<ApiActionDto>? modify = null)
    {
        var apiAction = new ApiActionDto
        {
            Action = "Test action",
            Name = "Test action",
        };

        apiAction.AddEndpoint();
        modify?.Invoke(apiAction);
        command.Dto.ApiActions.Add(apiAction);
        return command;
    }

    public static CreateDialogCommand AddGuiAction(
        this CreateDialogCommand command,
        Action<GuiActionDto>? modify = null)
    {
        var guiAction = new GuiActionDto
        {
            Action = "Test gui action",
            Url = new Uri("https://localhost"),
            AuthorizationAttribute = null,
            IsDeleteDialogAction = false,
            HttpMethod = HttpVerb.Values.GET,
            Priority = DialogGuiActionPriority.Values.Primary,
            Title =
            [
                new LocalizationDto
                {
                    Value = "gui action",
                    LanguageCode = "nb"
                }
            ],
            Prompt = null
        };

        modify?.Invoke(guiAction);
        command.Dto.GuiActions.Add(guiAction);
        return command;
    }

    public static ApiActionDto AddEndpoint(this ApiActionDto apiAction, Action<ApiActionEndpointDto>? modify = null)
    {
        var endpoint = new ApiActionEndpointDto
        {
            Url = new Uri("https://example.com"),
            HttpMethod = HttpVerb.Values.GET
        };

        modify?.Invoke(endpoint);
        apiAction.Endpoints.Add(endpoint);
        return apiAction;
    }
}
