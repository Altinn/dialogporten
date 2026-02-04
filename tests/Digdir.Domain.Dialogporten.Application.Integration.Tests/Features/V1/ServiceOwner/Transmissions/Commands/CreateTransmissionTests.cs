using System;
using System.Threading.Tasks;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.Extensions.DependencyInjection;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;
using TransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateTransmissionTests : ApplicationCollectionFixture
{
    public CreateTransmissionTests(DialogApplication application) : base(application) { }

    private const string ParentTransmissionIdKey = nameof(ParentTransmissionIdKey);

    [Fact]
    public Task Can_Create_Transmission_Related_To_Existing_Transmission() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((command, ctx) =>
            {
                var parentTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                parentTransmission.Id = parentTransmission.Id.CreateVersion7IfDefault();
                var parentId = parentTransmission.Id!.Value;
                ctx.Bag[ParentTransmissionIdKey] = parentId;
                command.Dto.Transmissions = [parentTransmission];
            })
            .AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) =>
            {
                var parentId = (Guid)ctx.Bag[ParentTransmissionIdKey]!;
                var transmission = new CreateTransmissionDto
                {
                    Id = Guid.CreateVersion7(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Type = DialogTransmissionType.Values.Information,
                    RelatedTransmissionId = parentId,
                    Sender = new()
                    {
                        ActorType = Domain.Actors.ActorType.Values.ServiceOwner
                    },
                    Content = new()
                    {
                        Title = new ContentValueDto
                        {
                            Value = [new LocalizationDto
                            {
                                LanguageCode = "nb",
                                Value = "Ny melding"
                            }]
                        }
                    }
                };

                return new CreateTransmissionCommand
                {
                    DialogId = ctx.GetDialogId(),
                    Transmissions = [transmission]
                };
            })
            .AssertResult<CreateTransmissionSuccess>()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                result.Transmissions.Should().HaveCount(2);
                result.Transmissions.Last().RelatedTransmissionId.Should()
                    .Be(result.Transmissions.First().Id);
            });
}
