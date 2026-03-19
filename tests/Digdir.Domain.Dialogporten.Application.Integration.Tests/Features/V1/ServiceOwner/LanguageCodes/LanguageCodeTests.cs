using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.LanguageCodes;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class LanguageCodeTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    public sealed record CreateDialogLanguageCodeScenario(
        string DisplayName,
        Action<CreateDialogCommand, FlowContext> ModifyCommand,
        bool ExpectSuccess) : ClassDataBase(DisplayName);

    private sealed class CreateDialogLanguageCodeTestData : TheoryData<CreateDialogLanguageCodeScenario>
    {
        public CreateDialogLanguageCodeTestData()
        {
            Add(new CreateDialogLanguageCodeScenario(
                DisplayName: "Valid language code nb",
                ModifyCommand: CreateTitleWithLanguageCode("nb"),
                ExpectSuccess: true));

            Add(new CreateDialogLanguageCodeScenario(
                DisplayName: "Valid language code nb_NO",
                ModifyCommand: CreateTitleWithLanguageCode("nb_NO"),
                ExpectSuccess: true));

            Add(new CreateDialogLanguageCodeScenario(
                DisplayName: "Valid language code en",
                ModifyCommand: CreateTitleWithLanguageCode("en"),
                ExpectSuccess: true));

            Add(new CreateDialogLanguageCodeScenario(
                DisplayName: "Valid language code en_US",
                ModifyCommand: CreateTitleWithLanguageCode("en_US"),
                ExpectSuccess: true));

            Add(new CreateDialogLanguageCodeScenario(
                DisplayName: "Invalid language code no",
                ModifyCommand: CreateTitleWithLanguageCode("no"),
                ExpectSuccess: false));

            Add(new CreateDialogLanguageCodeScenario(
                DisplayName: "Invalid language code invalid",
                ModifyCommand: CreateTitleWithLanguageCode("invalid"),
                ExpectSuccess: false));

            // We ignore region codes, so this should be valid
            Add(new CreateDialogLanguageCodeScenario(
                DisplayName: "Language code with region prefix should be valid",
                ModifyCommand: CreateTitleWithLanguageCode("nb_ignore_region_code"),
                ExpectSuccess: true));

            Add(new CreateDialogLanguageCodeScenario(
                DisplayName: "Missing language code should fail",
                ModifyCommand: CreateTitleWithLanguageCode(string.Empty),
                ExpectSuccess: false));
        }
    }

    [Theory, ClassData(typeof(CreateDialogLanguageCodeTestData))]
    public Task Can_Create_Localization_With_Valid_LanguageCode(CreateDialogLanguageCodeScenario scenario)
        => FlowBuilder.For(Application)
            .CreateSimpleDialog(scenario.ModifyCommand)
            .ExecuteAndAssert(x =>
            {
                if (scenario.ExpectSuccess)
                {
                    Assert.IsType<CreateDialogSuccess>(x);
                }
                else
                {
                    Assert.IsType<ValidationError>(x);
                    (x as ValidationError)!
                        .ShouldHaveErrorWithText("language code");
                }
            });

    private static Action<CreateDialogCommand, FlowContext> CreateTitleWithLanguageCode(string languageCode) =>
        (x, _) => x.Dto.Content!.Title = new ContentValueDto
        {
            Value = [new() { LanguageCode = languageCode, Value = "text" }]
        };
}
