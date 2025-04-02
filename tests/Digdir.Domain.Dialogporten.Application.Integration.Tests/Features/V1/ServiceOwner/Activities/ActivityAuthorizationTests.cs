using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Activities;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ActivityAuthorizationTests : ApplicationCollectionFixture
{
    public ActivityAuthorizationTests(DialogApplication application) : base(application) { }

    [Theory]
    [InlineData(DialogActivityType.Values.CorrespondenceOpened, true)]
    [InlineData(DialogActivityType.Values.CorrespondenceOpened, false)]
    [InlineData(DialogActivityType.Values.CorrespondenceConfirmed, true)]
    [InlineData(DialogActivityType.Values.CorrespondenceConfirmed, false)]
    public async Task Creating_Correspondence_Activities_Requires_Correct_Scope_When_Creating_Dialog(
        DialogActivityType.Values activityType, bool hasScope)
    {
        // Arrange
        var userWithLegacyScope = new IntegrationTestUser([new("scope", AuthorizationScope.CorrespondenceScope)]);
        Application.ConfigureServices(services =>
        {
            if (!hasScope) return;
            services.RemoveAll<IUser>();
            services.AddSingleton<IUser>(userWithLegacyScope);
        });

        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: activityType);

        createCommand.Dto.Activities.Add(activity);

        // Act
        var createResponse = await Application.Send(createCommand);

        // Assert
        if (hasScope)
        {
            createResponse.TryPickT0(out var success, out _).Should().BeTrue();
            success.DialogId.Should().NotBeEmpty();
            var activityEntities = await Application.GetDbEntities<DialogActivity>();
            activityEntities.Should().HaveCount(1);
            activityEntities.First().TypeId.Should().Be(activityType);
        }
        else
        {
            createResponse.TryPickT3(out var forbidden, out _).Should().BeTrue();
            forbidden.Should().NotBeNull();
            forbidden.Reasons
                .Should()
                .ContainSingle(x =>
                    x.Contains(AuthorizationScope.CorrespondenceScope) &&
                    x.Contains(nameof(DialogActivityType.Values.CorrespondenceOpened)) &&
                    x.Contains(nameof(DialogActivityType.Values.CorrespondenceConfirmed)
                    ));
        }
    }

    [Theory]
    [InlineData(DialogActivityType.Values.CorrespondenceOpened, true)]
    [InlineData(DialogActivityType.Values.CorrespondenceOpened, false)]
    [InlineData(DialogActivityType.Values.CorrespondenceConfirmed, true)]
    [InlineData(DialogActivityType.Values.CorrespondenceConfirmed, false)]
    public async Task Creating_Correspondence_Activities_Requires_Correct_Scope_When_Updating_Dialog(
        DialogActivityType.Values activityType, bool hasScope)
    {
        // Arrange
        var userWithLegacyScope = new IntegrationTestUser([new("scope", AuthorizationScope.CorrespondenceScope)]);
        Application.ConfigureServices(services =>
        {
            if (!hasScope) return;
            services.RemoveAll<IUser>();
            services.AddSingleton<IUser>(userWithLegacyScope);
        });

        var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        updateDialogDto.Activities.Add(new ActivityDto
        {
            Type = activityType,
            PerformedBy = new ActorDto { ActorType = ActorType.Values.ServiceOwner }
        });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        if (hasScope)
        {
            updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
            success.Should().NotBeNull();
            success.Revision.Should().NotBeEmpty();
            success.Revision.Should().NotBe(getDialogDto.AsT0.Revision);
            var activityEntities = await Application.GetDbEntities<DialogActivity>();
            activityEntities.Should().HaveCount(1);
            activityEntities.First().TypeId.Should().Be(activityType);
        }
        else
        {
            updateResponse.TryPickT4(out var forbidden, out _).Should().BeTrue();
            forbidden.Should().NotBeNull();
            forbidden.Reasons
                .Should()
                .ContainSingle(x =>
                    x.Contains(AuthorizationScope.CorrespondenceScope) &&
                    x.Contains(nameof(DialogActivityType.Values.CorrespondenceOpened)) &&
                    x.Contains(nameof(DialogActivityType.Values.CorrespondenceConfirmed)
                    ));
        }
    }


    // [Fact]
    // public async Task UpdateDialogCommand_Should_Set_New_Revision_If_IsSilentUpdate_Is_Set()
    // {
    //     // Arrange
    //     var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());
    //
    //     var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
    //     var getDialogDto = await Application.Send(getDialogQuery);
    //     var oldRevision = getDialogDto.AsT0.Revision;
    //
    //     var mapper = Application.GetMapper();
    //     var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
    //
    //     // Update progress
    //     updateDialogDto.Progress = (updateDialogDto.Progress % 100) + 1;
    //
    //     // Act
    //     var updateResponse = await Application.Send(new UpdateDialogCommand
    //     {
    //         Id = createCommandResponse.AsT0.DialogId,
    //         Dto = updateDialogDto,
    //         IsSilentUpdate = true
    //     });
    //
    //     // Assert
    //     updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
    //     success.Should().NotBeNull();
    //     success.Revision.Should().NotBeEmpty();
    //     success.Revision.Should().NotBe(oldRevision);
    // }
}
