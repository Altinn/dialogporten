using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using NSubstitute;
using UpdateDialogActivityDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ActivityDto;
using CreateDialogActivityDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ActivityDto;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.ServiceOwner.Activities;

public class ActivityValidatorTests
{
    public static IEnumerable<object[]> ActivityTypes() =>
        from DialogActivityType.Values activityType in Enum.GetValues<DialogActivityType.Values>()
        select new object[] { activityType, };

    [Theory, MemberData(nameof(ActivityTypes))]
    public void Only_TransmissionOpened_Is_Allowed_To_Set_TransmissionId(
        DialogActivityType.Values activityType)
    {
        // Arrange
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: activityType);
        activity.TransmissionId = IdentifiableExtensions.CreateVersion7();

        var user = Substitute.For<IUser>();
        var actorValidator = new ActorValidator();
        var localizationValidator = new LocalizationDtosValidatorFactory(user);

        var clock = new Clock();
        var createValidator = new CreateDialogDialogActivityDtoValidator(actorValidator, localizationValidator, clock);
        var updateValidator = new UpdateDialogDialogActivityDtoValidator(actorValidator, localizationValidator, clock);
        var createActivityValidator = new CreateActivityDtoValidator(actorValidator, localizationValidator, clock);

        // Act
        var createValidation = createValidator.Validate(activity);
        var updateValidation = updateValidator.Validate(ToUpdateActivityDto(activity));
        var createActivityValidation = createActivityValidator.Validate(ToCreateActivityDto(activity));

        // Assert
        if (activityType == DialogActivityType.Values.TransmissionOpened)
        {
            createValidation.IsValid.Should().BeTrue();
            updateValidation.IsValid.Should().BeTrue();
            createActivityValidation.IsValid.Should().BeTrue();
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

    // The activity DTOs share the same shape across the create/update/create-activity commands; these
    // replace the by-convention AutoMapper maps the test previously used (issue #967).
    private static UpdateDialogActivityDto ToUpdateActivityDto(CreateDialogActivityDto source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description
    };

    private static CreateActivityDto ToCreateActivityDto(CreateDialogActivityDto source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description
    };
}
