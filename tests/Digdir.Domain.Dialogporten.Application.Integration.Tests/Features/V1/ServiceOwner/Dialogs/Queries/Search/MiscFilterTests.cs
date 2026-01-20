using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class MiscFilterTests : ApplicationCollectionFixture
{
    public MiscFilterTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Should_Filter_On_ServiceResource()
    {
        const string matchingServiceResource = "urn:altinn:resource:service-resource-match";
        const string otherServiceResource = "urn:altinn:resource:service-resource-other";
        var matchingDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Party = Party;
                x.Dto.ServiceResource = otherServiceResource;
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = matchingDialogId;
                x.Dto.Party = Party;
                x.Dto.ServiceResource = matchingServiceResource;
            })
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.ServiceResource = [matchingServiceResource];
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
                result.Items.Should().ContainSingle(dto => dto.Id == matchingDialogId));
    }

    [Fact]
    public async Task Should_Filter_On_SystemLabel()
    {
        var matchingDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Party = Party;
                x.Dto.SystemLabel = SystemLabel.Values.Default;
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = matchingDialogId;
                x.Dto.Party = Party;
                x.Dto.SystemLabel = SystemLabel.Values.Bin;
            })
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.SystemLabel = [SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
                result.Items.Should().ContainSingle(dto => dto.Id == matchingDialogId));
    }

    [Fact]
    public async Task Should_Filter_On_ExtendedStatus()
    {
        const string targetExtendedStatus = "ext-status";
        var matchingDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Party = Party;
                x.Dto.ExtendedStatus = "other-status";
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = matchingDialogId;
                x.Dto.Party = Party;
                x.Dto.ExtendedStatus = targetExtendedStatus;
            })
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.ExtendedStatus = [targetExtendedStatus];
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
                result.Items.Should().ContainSingle(dto => dto.Id == matchingDialogId));
    }

    [Fact]
    public async Task Should_Filter_On_ExternalReference()
    {
        const string targetExternalReference = "external-ref";
        var matchingDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Party = Party;
                x.Dto.ExternalReference = "other-ref";
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = matchingDialogId;
                x.Dto.Party = Party;
                x.Dto.ExternalReference = targetExternalReference;
            })
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.ExternalReference = targetExternalReference;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
                result.Items.Should().ContainSingle(dto => dto.Id == matchingDialogId));
    }

    [Fact]
    public async Task Should_Filter_On_Status()
    {
        var matchingDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Party = Party;
                x.Dto.Status = DialogStatusInput.Awaiting;
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = matchingDialogId;
                x.Dto.Party = Party;
                x.Dto.Status = DialogStatusInput.Completed;
            })
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.Status = [DialogStatus.Values.Completed];
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
                result.Items.Should().ContainSingle(dto => dto.Id == matchingDialogId));
    }

    [Fact]
    public async Task Should_Filter_On_Process()
    {
        const string targetProcess = "matching-process";
        var matchingDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Party = Party;
                x.Dto.Process = "other-process";
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = matchingDialogId;
                x.Dto.Party = Party;
                x.Dto.Process = targetProcess;
            })
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.Process = targetProcess;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
                result.Items.Should().ContainSingle(dto => dto.Id == matchingDialogId));
    }

    [Fact]
    public async Task ExcludeApiOnly_Should_Filter_Out_ApiOnly_Dialogs()
    {
        var apiOnlyDialogId = NewUuidV7();
        var normalDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = apiOnlyDialogId;
                x.Dto.Party = Party;
                x.Dto.IsApiOnly = true;
            })
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = normalDialogId;
                x.Dto.Party = Party;
                x.Dto.IsApiOnly = false;
            })
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.ExcludeApiOnly = true;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
                result.Items.Should().ContainSingle(dto => dto.Id == normalDialogId));
    }

    [Fact]
    public async Task Deleted_Filter_Only_Should_Return_SoftDeleted_Dialogs()
    {
        var deletedDialogId = NewUuidV7();
        var activeDialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = deletedDialogId;
                x.Dto.Party = Party;
            })
            .DeleteDialog()
            .CreateSimpleDialog(x =>
            {
                x.Dto.Id = activeDialogId;
                x.Dto.Party = Party;
            })
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.Deleted = DeletedFilter.Only;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
            {
                result.Items.Should().ContainSingle(dto => dto.Id == deletedDialogId);
                result.Items.Should().NotContain(dto => dto.Id == activeDialogId);
            });
    }
}
