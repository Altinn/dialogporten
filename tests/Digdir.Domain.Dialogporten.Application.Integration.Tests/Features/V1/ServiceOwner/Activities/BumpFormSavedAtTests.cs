using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.BumpFormSaved;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Activities;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class BumpFormSavedAtTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Can_Bump_FormSavedAt_When_Dialog_Id_Draft()
    {
        var timestamp = DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Status = DialogStatusInput.Draft;
                x.Dto.Activities =
                [
                    new ActivityDto
                    {
                        Type = DialogActivityType.Values.FormSaved,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.ServiceOwner
                        },
                        CreatedAt = timestamp
                    },
                    new ActivityDto
                    {
                        CreatedAt = timestamp - TimeSpan.FromDays(1),
                        Type = DialogActivityType.Values.Information,
                        PerformedBy = new ActorDto
                        {
                            ActorType = ActorType.Values.PartyRepresentative,
                            ActorName = "Fredrik"
                        },
                        Description =
                        [
                            new LocalizationDto
                            {
                                Value = "Desc",
                                LanguageCode = "nb"
                            }
                        ]
                    }
                ];
                x.Dto.CreatedAt = timestamp;
            })
            .BumpFormSaved()
            .SendCommand(ctx => new GetDialogQuery
            {
                DialogId = ctx.GetDialogId(),
            })
            .ExecuteAndAssert<DialogDto>(x => x.Activities.Last().CreatedAt.Should().BeMoreThan(timestamp.Offset));
    }

    [Fact]
    public async Task Cannot_Bump_FormSavedAt_When_FormSaved_Is_Not_Latest_Activity()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Status = DialogStatusInput.Draft)
            .BumpFormSaved()
            .ExecuteAndAssert<DomainError>(x => x.ShouldHaveErrorWithText("Latest activity is not of type FormSaved"));
    }


    [Fact]
    public async Task Cannot_Bump_FormSavedAt_When_Dialog_Is_Not_Draft()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Status = DialogStatusInput.InProgress)
            .BumpFormSaved()
            .ExecuteAndAssert<DomainError>(x => x.ShouldHaveErrorWithText("Can only bump timestamp when dialog status is Draft"));
    }
}
