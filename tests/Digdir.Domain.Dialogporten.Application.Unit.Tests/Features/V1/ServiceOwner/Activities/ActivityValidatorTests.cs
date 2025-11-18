using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

using UpdateActivityDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ActivityDto;
using CreateActivityDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ActivityDto;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.ServiceOwner.Activities;

public class ActivityValidatorTests
{
    public static IEnumerable<object[]> ActivityTypes() =>
        from DialogActivityType.Values activityType in Enum.GetValues(typeof(DialogActivityType.Values))
        select new object[] { activityType, };

    [Theory, MemberData(nameof(ActivityTypes))]
    public void Only_TransmissionOpened_Is_Allowed_To_Set_TransmissionId(
        DialogActivityType.Values activityType)
    {
        // Arrange
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<CreateActivityDto, UpdateActivityDto>()).CreateMapper();

        var activity = DialogGenerator.GenerateFakeDialogActivity(type: activityType);
        activity.TransmissionId = IdentifiableExtensions.CreateVersion7();

        var localizationValidator = new LocalizationDtosValidator();
        var actorValidator = new ActorValidator();

        var clock = new Clock();
        var createValidator = new CreateDialogDialogActivityDtoValidator(localizationValidator, actorValidator, clock);
        var updateValidator = new UpdateDialogDialogActivityDtoValidator(localizationValidator, actorValidator, clock);

        // Act
        var createValidation = createValidator.Validate(activity);
        var updateValidation = updateValidator.Validate(mapper.Map<UpdateActivityDto>(activity));

        // Assert
        if (activityType == DialogActivityType.Values.TransmissionOpened)
        {
            createValidation.IsValid.Should().BeTrue();
            updateValidation.IsValid.Should().BeTrue();
        }
        else
        {
            createValidation.IsValid.Should().BeFalse();
            updateValidation.IsValid.Should().BeFalse();

            createValidation.Errors.Should().ContainSingle();
            updateValidation.Errors.Should().ContainSingle();

            createValidation.Errors.First().ErrorMessage.Should().Contain("TransmissionOpened");
            updateValidation.Errors.First().ErrorMessage.Should().Contain("TransmissionOpened");
        }
    }
}
