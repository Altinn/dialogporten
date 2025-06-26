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
    public async Task Can_Bump_FormSavedAt_Without_Updating_UpdatedAt()
    {
        var dialogCreatedAt = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        var activitiesCreatedAt = dialogCreatedAt - TimeSpan.FromDays(2);
        var newFormSavedAt = dialogCreatedAt - TimeSpan.FromDays(1);

        var formSavedActivityId = Guid.CreateVersion7();
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.CreatedAt = dialogCreatedAt;
                x.Dto.UpdatedAt = dialogCreatedAt;
                x.Dto.Status = DialogStatusInput.Draft;
                x.AddActivity(DialogActivityType.Values.FormSaved, x =>
                {
                    x.CreatedAt = activitiesCreatedAt;
                    x.Id = formSavedActivityId;
                });
                x.AddActivity(DialogActivityType.Values.Information);
                x.Dto.CreatedAt = activitiesCreatedAt;
            })
            .BumpFormSaved(x =>
            {
                x.ActivityId = formSavedActivityId;
                x.NewCreatedAt = newFormSavedAt;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var activity = x.Activities.Single(activity => activity.Id == formSavedActivityId);
                activity.Should().NotBeNull();

                activity!.CreatedAt.Should().Be(newFormSavedAt);
                activity.Type.Should().Be(DialogActivityType.Values.FormSaved);

                x.UpdatedAt.Should().BeCloseTo(dialogCreatedAt, TimeSpan.FromMilliseconds(1));
            });
    }

    [Fact]
    public async Task Cannot_Bump_Activity_Without_Type_FormSaved()
    {
        var activityId = Guid.CreateVersion7();
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddActivity(DialogActivityType.Values.Information, x => x.Id = activityId)
            ).BumpFormSaved(x => x.ActivityId = activityId)
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText($"Only {nameof(DialogActivityType.Values.FormSaved)} activities is allowed to be updated using admin scope."));
    }
}
