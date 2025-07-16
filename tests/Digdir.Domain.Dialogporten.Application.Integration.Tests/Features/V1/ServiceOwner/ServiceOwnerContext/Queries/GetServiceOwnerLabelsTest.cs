using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabels;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using ServiceOwnerLabelDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ServiceOwnerLabelDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetServiceOwnerLabelsTest : ApplicationCollectionFixture
{
    public GetServiceOwnerLabelsTest(DialogApplication application) : base(application) { }

    [Fact]
    public Task Can_Get_ServiceOwnerLabels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.ServiceOwnerContext!.ServiceOwnerLabels
                = CreateLabels("Scadrial", "Roshar", "Sel"))
            .SendCommand((_, ctx) => new GetServiceOwnerLabelsQuery { DialogId = ctx.GetDialogId() })
            .ExecuteAndAssert<ServiceOwnerLabelResultDto>(x => x.Labels.Should().HaveCount(3));

    [Fact]
    public Task Get_ServiceOwnerLabels_With_Invalid_DialogId_Returns_NotFound() =>
        FlowBuilder.For(Application)
            .SendCommand(_ => new GetServiceOwnerLabelsQuery { DialogId = NewUuidV7() })
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();

    private static List<ServiceOwnerLabelDto> CreateLabels(params string[] labels) =>
        labels.Select(label => new ServiceOwnerLabelDto { Value = label }).ToList();
}
