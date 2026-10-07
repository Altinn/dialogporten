using System.Diagnostics;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.SaveChangesTestInterceptors;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry.IPartyNameRegistryTransport;
using ActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetActivity.ActivityDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Persistence;

[Collection(nameof(DialogCqrsCollectionFixture))]
public sealed class DuplicateActorNameConflictTests(DialogApplication application)
    : ApplicationCollectionFixture(application)
{

    [Fact]
    public async Task Save_Recovers_When_Save_ActorName_Races()
    {
        var actorId = "urn:altinn:person:identifier-no:09095614407";

        await FlowBuilder.For(Application)
            .WithSaveChangesInterceptor<ProvokeActorNameRaceConditionSaveInterceptor>()
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>()
            .CreateActivity((command, _) => command.Activity = new CreateActivityDto
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                ExtendedType = null,
                Type = DialogActivityType.Values.DialogCreated,
                TransmissionId = null,
                PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = null,
                    ActorId = actorId
                },
                Description = []
            })
            .AssertResult<CreateActivitySuccess>()
            .GetActivity()
            .ExecuteAndAssert<ActivityDto>((result, ctx) =>
            {
                var interceptor = ctx.Application
                    .GetServiceProvider()
                    .GetServices<ISaveChangesTestInterceptor>()
                    .OfType<ProvokeActorNameRaceConditionSaveInterceptor>()
                    .Single();

                interceptor.HasRaced.Should().Be(true);
                result.PerformedBy.ActorId.Should().Be(actorId);
                result.PerformedBy.ActorName.Should().Be("Brando Sando");
            });
    }

    [Theory]
    [InlineData("InternalServerError")]
    [InlineData("HttpRequestException")]
    public async Task Save_Recovers_When_Save_ActorName_Races_When_Party_Name_Registry_Is_Down(string failHow)
    {
        var actorId = "urn:altinn:person:identifier-no:09095614407";

        await FlowBuilder.For(Application)
            .WithSaveChangesInterceptor<ProvokeActorNameRaceConditionSaveInterceptor>()
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>()
            .ConfigurePartyNameRegistry(p =>
            {
                switch (failHow)
                {
                    case "InternalServerError":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Returns(TestPartyNameRegistry.InternalServerError);
                        break;
                    case "HttpRequestException":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Throws(new HttpRequestException());
                        break;
                    default: throw new UnreachableException($"Uknown failhow {failHow}");
                }
            })
            .CreateActivity((command, _) => command.Activity = new CreateActivityDto
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero),
                ExtendedType = null,
                Type = DialogActivityType.Values.DialogCreated,
                TransmissionId = null,
                PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = null,
                    ActorId = actorId
                },
                Description = []
            })
            .AssertResult<CreateActivitySuccess>()
            .GetActivity()
            .ExecuteAndAssert<ActivityDto>((result, ctx) =>
            {
                var interceptor = ctx.Application
                    .GetServiceProvider()
                    .GetServices<ISaveChangesTestInterceptor>()
                    .OfType<ProvokeActorNameRaceConditionSaveInterceptor>()
                    .Single();

                interceptor.HasRaced.Should().Be(true);
                result.PerformedBy.ActorId.Should().Be(actorId);
                result.PerformedBy.ActorName.Should().Be(null);
            });
    }
}
