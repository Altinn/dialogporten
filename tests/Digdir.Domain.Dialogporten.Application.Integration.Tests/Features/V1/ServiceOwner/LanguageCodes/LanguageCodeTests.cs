using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.LanguageCodes;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class LanguageCodeTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private sealed class CreateDialogLanguageCodeTestData : TheoryData<Action<CreateDialogCommand>, bool>
    {
        public CreateDialogLanguageCodeTestData()
        {
            Add(CreateTitleWithLanguageCode("nb"), true);
            Add(CreateTitleWithLanguageCode("nb_NO"), true);
            Add(CreateTitleWithLanguageCode("en"), true);
            Add(CreateTitleWithLanguageCode("en_US"), true);
            Add(CreateTitleWithLanguageCode("no"), false);
            Add(CreateTitleWithLanguageCode("invalid"), false);
            // We ignore region codes, so this should be valid
            Add(CreateTitleWithLanguageCode("nb_ignore_region_code"), true);
            Add(CreateTitleWithLanguageCode(string.Empty), false);
        }
    }

    [Theory, ClassData(typeof(CreateDialogLanguageCodeTestData))]
    public Task Can_Create_Localization_With_Valid_LanguageCode(
        Action<CreateDialogCommand> modifyCommand,
        bool shouldSucceed)
        => FlowBuilder.For(Application)
            .CreateSimpleDialog(modifyCommand)
            .ExecuteAndAssert(x =>
            {
                if (shouldSucceed)
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

    private static Action<CreateDialogCommand> CreateTitleWithLanguageCode(string languageCode) =>
        x => x.Dto.Content!.Title = new ContentValueDto
        {
            Value = [new() { LanguageCode = languageCode, Value = "text" }]
        };
}
