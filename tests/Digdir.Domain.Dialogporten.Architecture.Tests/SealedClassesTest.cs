using NetArchTest.Rules;

namespace Digdir.Domain.Dialogporten.Architecture.Tests;

public class SealedClassesTest
{
    [Fact]
    public void All_Classes_Without_Inheritors_Should_Be_Sealed()
    {
        var nonAbstractClasses = Types
            .InAssemblies(DialogportenAssemblies.All)
            .That().AreClasses()
            .And().AreNotAbstract()
            .And().DoNotHaveName(nameof(Program))
            .GetTypes()
            .ToList();

        var inheritedTypes = nonAbstractClasses
            .Where(x => x.BaseType is not null)
            .Select(t => t.BaseType!.IsGenericType
                ? t.BaseType.GetGenericTypeDefinition()
                : t.BaseType)
            .ToHashSet();

        var notSealedNorInheritedTypes = nonAbstractClasses
            .Except(inheritedTypes)
            .Where(x => !x.IsSealed)
            .ToList();

        Assert.Empty(notSealedNorInheritedTypes);
    }
}
