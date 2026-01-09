using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

public class CreateDialogTests
{
    [Fact]
    public async Task CreateDialogCommand_Should_Return_Forbidden_When_Scope_Is_Missing()
    {
        // Arrange
        var dialogDbContextSub = Substitute.For<IDialogDbContext>();

        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(CreateDialogCommandHandler).Assembly);
        }).CreateMapper();

        var unitOfWorkSub = Substitute.For<IUnitOfWork>();
        var domainContextSub = Substitute.For<IDomainContext>();
        var resourceRegistrySub = Substitute.For<IResourceRegistry>();
        var serviceAuthorizationSub = Substitute.For<IServiceResourceAuthorizer>();
        var userSub = Substitute.For<IUser>();
        var clock = new Clock();

        var hierarchyValidatorSub = Substitute.For<ITransmissionHierarchyValidator>();
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();

        serviceAuthorizationSub
            .AuthorizeServiceResources(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
            .Returns(new Forbidden());

        resourceRegistrySub
            .GetResourceInformation(createCommand.Dto.ServiceResource, Arg.Any<CancellationToken>())
            .Returns(new ServiceResourceInformation(createCommand.Dto.ServiceResource, "foo", "912345678", "ttd"));

        var commandHandler = new CreateDialogCommandHandler(userSub, clock, dialogDbContextSub,
            mapper, unitOfWorkSub, domainContextSub,
            resourceRegistrySub, serviceAuthorizationSub, hierarchyValidatorSub);

        // Act
        var result = await commandHandler.Handle(createCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsT3);
    }

    [Fact]
    public async Task CreateDialogCommand_Should_Return_Forbidden_When_User_Is_Not_Owner()
    {
        // Arrange
        var dialogDbContextSub = Substitute.For<IDialogDbContext>();

        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(CreateDialogCommandHandler).Assembly);
        }).CreateMapper();

        var unitOfWorkSub = Substitute.For<IUnitOfWork>();
        var domainContextSub = Substitute.For<IDomainContext>();
        var resourceRegistrySub = Substitute.For<IResourceRegistry>();
        var serviceAuthorizationSub = Substitute.For<IServiceResourceAuthorizer>();
        var userSub = Substitute.For<IUser>();
        var hierarchyValidatorSub = Substitute.For<ITransmissionHierarchyValidator>();
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var clock = new Clock();

        serviceAuthorizationSub
            .AuthorizeServiceResources(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
            .Returns(new Forbidden());

        resourceRegistrySub
            .GetResourceInformation(createCommand.Dto.ServiceResource, Arg.Any<CancellationToken>())
            .Returns(new ServiceResourceInformation(createCommand.Dto.ServiceResource, "foo", "912345678", "ttd"));

        var commandHandler = new CreateDialogCommandHandler(userSub, clock, dialogDbContextSub,
            mapper, unitOfWorkSub, domainContextSub,
            resourceRegistrySub, serviceAuthorizationSub, hierarchyValidatorSub);

        // Act
        var result = await commandHandler.Handle(createCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsT3); // Forbidden
    }
}
