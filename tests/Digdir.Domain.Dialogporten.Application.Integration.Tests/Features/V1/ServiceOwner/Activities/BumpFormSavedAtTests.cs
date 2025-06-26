using Castle.Components.DictionaryAdapter.Xml;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Activities;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class BumpFormSavedAtTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Updating_Activity_CreatedAt_Will_Not_Update_Dialog_UpdatedAt_When_LessThan()
    {
        var dialogCreatedAt = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        var formCreatedAt = dialogCreatedAt - TimeSpan.FromDays(2);
        var newFormCreatedAt = dialogCreatedAt - TimeSpan.FromDays(1);

        var formSavedActivityId = Guid.CreateVersion7();
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.CreatedAt = dialogCreatedAt;
                x.Dto.UpdatedAt = dialogCreatedAt;
                x.AddActivity(DialogActivityType.Values.FormSaved, x =>
                {
                    x.CreatedAt = formCreatedAt;
                    x.Id = formSavedActivityId;
                });
            })
            .UpdateFormSavedActivityTime(x =>
            {
                x.ActivityId = formSavedActivityId;
                x.NewCreatedAt = newFormCreatedAt;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Activities
                    .Single(activity => activity.Id == formSavedActivityId)
                    .CreatedAt.Should().Be(newFormCreatedAt);

                x.UpdatedAt.Should().BeCloseTo(dialogCreatedAt, TimeSpan.FromMilliseconds(1));
            });
    }

    [Fact]
    public async Task Updating_Activity_CreatedAt_Will_Update_Dialog_UpdatedAt_When_GreaterThan()
    {

        var dialogCreatedAt = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        var formCreatedAt = dialogCreatedAt - TimeSpan.FromDays(2);
        var newFormCreatedAt = DateTimeOffset.UtcNow;

        var formSavedActivityId = Guid.CreateVersion7();
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.CreatedAt = dialogCreatedAt;
                x.Dto.UpdatedAt = dialogCreatedAt;
                x.AddActivity(DialogActivityType.Values.FormSaved, x =>
                {
                    x.Id = formSavedActivityId;
                    x.CreatedAt = formCreatedAt;
                });
            })
            .UpdateFormSavedActivityTime(x =>
            {
                x.ActivityId = formSavedActivityId;
                x.NewCreatedAt = newFormCreatedAt;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Activities
                    .Single(x => x.Id == formSavedActivityId)
                    .CreatedAt.Should().Be(newFormCreatedAt);

                x.UpdatedAt.Should().BeCloseTo(newFormCreatedAt, TimeSpan.FromMilliseconds(1));
            });
    }

    [Fact]
    public async Task Updating_Activity_CreatedAt_Will_Update_Dialog_Revision()
    {
        var activityId = Guid.CreateVersion7();

        Guid? initialRevision = null;
        var success = await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddActivity(DialogActivityType.Values.FormSaved, x =>
                {
                    x.Id = activityId;
                });
            })
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>(x => initialRevision = x.Revision)
            .UpdateFormSavedActivityTime(x =>
            {
                x.ActivityId = activityId;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>();
        success.Revision.Should().NotBeEmpty();
        success.Revision.Should().NotBe(initialRevision!.Value);

    }

    [Fact]
    public async Task Cannot_Update_Activity_CreatedAt_When_Activity_Not_Of_Type_FormSaved()
    {
        var activityId = Guid.CreateVersion7();
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddActivity(DialogActivityType.Values.Information, x => x.Id = activityId)
            ).UpdateFormSavedActivityTime(x => x.ActivityId = activityId)
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText($"Only {nameof(DialogActivityType.Values.FormSaved)} activities is allowed to be updated using admin scope."));
    }
}
