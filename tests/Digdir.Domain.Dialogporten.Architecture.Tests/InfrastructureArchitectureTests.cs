using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Exceptions;
using FluentAssertions;
using FluentValidation;
using NetArchTest.Rules;

namespace Digdir.Domain.Dialogporten.Architecture.Tests;

public class InfrastructureArchitectureTests
{
    [Fact]
    public void All_Classes_In_Infrastructure_Should_Be_Internal()
    {
        var publicByDesignClasses = new[]
        {
            nameof(InfrastructureAssemblyMarker),
            nameof(InfrastructureExtensions),

            // These classes are currently public but should be internal, moved to another assembly, or deleted
            nameof(IUpstreamServiceError)
        };

        var publicClasses = Types
            .InAssembly(InfrastructureAssemblyMarker.Assembly)
            .That().DoNotResideInNamespaceMatching("Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations")
            .And().DoNotHaveNameEndingWith("Settings")
            .And().DoNotHaveNameEndingWith("Constants")
            .And().AreNotInterfaces()
            .And().DoNotHaveName(publicByDesignClasses)
            .Should().NotBePublic()
            .GetResult();

        publicClasses.FailingTypes.Should().BeNullOrEmpty();
        publicClasses.IsSuccessful.Should().BeTrue();
    }


    [Fact]
    public void All_Validators_Should_Be_Internal()
    {
        var validatorTypes = Types
            .InAssemblies(DialogportenAssemblies.All)
            .That().AreClasses()
            .And().AreNotAbstract()
            .And().Inherit(typeof(AbstractValidator<>))
            .GetTypes();

        var publicValidators = validatorTypes
            .Where(t => t.IsPublic)
            .ToList();

        publicValidators.Should().BeEmpty(
            $"These validators are public but should be internal: " +
            $"{string.Join(", ", publicValidators.Select(t => t.FullName))}");
    }
}
