using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using FluentAssertions;
using ServiceOwnerLabelDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ServiceOwnerLabelDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.
    Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogServiceOwnerLabelTests : ApplicationCollectionFixture
{
    public CreateDialogServiceOwnerLabelTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Can_Create_Dialog_With_ServiceOwner_Labels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x
                .AddServiceOwnerLabels("Scadrial", "Roshar", "Sel"))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.ServiceOwnerContext
                    .ServiceOwnerLabels
                    .Count
                    .Should()
                    .Be(3));

    [Fact]
    public Task Cannot_Create_Dialog_With_Duplicate_ServiceOwner_Labels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x
                // Case-insensitive, duplicate labels
                .AddServiceOwnerLabels("sel", "SEL"))
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText("duplicate"));

    [Fact]
    public Task Cannot_Create_Labels_With_Invalid_Length() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddServiceOwnerLabels(
                    null!,
                    new string('a', Constants.MinSearchStringLength - 1),
                    new string('a', Constants.DefaultMaxStringLength + 1)))
            .ExecuteAndAssert<ValidationError>(x =>
            {
                x.ShouldHaveErrorWithText("not be empty");
                x.ShouldHaveErrorWithText("at least");
                x.ShouldHaveErrorWithText("or fewer");
            });

    [Fact]
    public Task Cannot_Create_More_Than_Maximum_Allowed_ServiceOwner_Labels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels =
                    Enumerable.Range(0, DialogServiceOwnerLabel.MaxNumberOfLabels + 1)
                        .Select(i => new ServiceOwnerLabelDto { Value = $"label{i}" })
                        .ToList();
            })
            .ExecuteAndAssert<ValidationError>(x =>
            {
                x.ShouldHaveErrorWithText("Maximum");
                x.ShouldHaveErrorWithText($"{DialogServiceOwnerLabel.MaxNumberOfLabels}");
            });
}
