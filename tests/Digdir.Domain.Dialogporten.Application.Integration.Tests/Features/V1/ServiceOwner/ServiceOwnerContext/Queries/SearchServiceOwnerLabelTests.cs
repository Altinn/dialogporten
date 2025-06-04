using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchServiceOwnerLabelTests : ApplicationCollectionFixture
{
    public SearchServiceOwnerLabelTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Can_Filter_On_Single_Label()
    {
        const string label = "Scadrial";
        var labeledDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = labeledDialogId;
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = label }];
            })
            .SearchServiceOwnerDialogs(x => x.ServiceOwnerLabels = [label])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                x.Items.Should().HaveCount(1);
                x.Items[0].Id.Should().Be(labeledDialogId);
            });
    }

    [Fact]
    public async Task Multiple_Label_Inputs_Must_All_Match()
    {
        const string label1 = "Scadrial";
        const string label2 = "Roshar";

        var dialogIdMatchingBothLabels = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog(x =>
            {
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels =
                [
                    new() { Value = label1 }
                ];
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = dialogIdMatchingBothLabels;
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels =
                [
                    new() { Value = label1 },
                    new() { Value = label2 }
                ];
            })
            .SearchServiceOwnerDialogs(x => x.ServiceOwnerLabels = [label1, label2])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                x.Items.Should().HaveCount(1);
                x.Items[0].Id.Should().Be(dialogIdMatchingBothLabels);
            });
    }

    [Fact]
    public async Task Can_Filter_On_Multiple_Labels_With_Prefix()
    {
        const string scadrialOne = "ScadrialOne";
        const string scadrialTwo = "ScadrialTwo";
        const string roshar = "Roshar";
        const string adonalsium = "Adonalsium";

        var dialogIdMatchingAllSearchCriteria = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels =
                [
                    new() { Value = scadrialOne },
                    new() { Value = scadrialTwo },
                    new() { Value = adonalsium }
                ];
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = dialogIdMatchingAllSearchCriteria;
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels =
                [
                    new() { Value = roshar },
                    new() { Value = scadrialTwo },
                    new() { Value = adonalsium }
                ];
            })
            .SearchServiceOwnerDialogs(x => x.ServiceOwnerLabels =
                // Only the second dialog should match all the search criteria
                ["Scadrial*", "Roshar", "Adon*"])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                x.Items.Should().HaveCount(1);
                x.Items[0].Id.Should().Be(dialogIdMatchingAllSearchCriteria);
            });
    }

    [Fact]
    public async Task Filtering_On_Non_Existing_Label_Returns_No_Results()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog(x =>
            {
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels =
                [
                    new() { Value = "ScadrialOne" }
                ];
            })
            .SearchServiceOwnerDialogs(x => x.ServiceOwnerLabels = ["Scadrial"])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x => { x.Items.Should().BeEmpty(); });
    }

    [Fact]
    public Task Cannot_Filter_On_Invalid_Label_Length() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogs(x =>
            {
                x.ServiceOwnerLabels =
                [
                    new string('a', Constants.MinSearchStringLength - 1),
                    new string('a', Constants.DefaultMaxStringLength + 1)
                ];
            })
            .ExecuteAndAssert<ValidationError>(x =>
            {
                x.ShouldHaveErrorWithText("at least");
                x.ShouldHaveErrorWithText("or fewer");
            });
}
