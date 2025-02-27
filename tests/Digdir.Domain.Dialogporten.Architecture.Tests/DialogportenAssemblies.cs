using System.Reflection;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.GraphQL;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Janitor;
using Digdir.Domain.Dialogporten.Service;
using Digdir.Domain.Dialogporten.WebApi;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Architecture.Tests;

public static class DialogportenAssemblies
{
    internal static readonly List<Assembly> All =
    [
        ApplicationAssemblyMarker.Assembly,
        DomainAssemblyMarker.Assembly,
        GraphQLAssemblyMarker.Assembly,
        InfrastructureAssemblyMarker.Assembly,
        JanitorAssemblyMarker.Assembly,
        ServiceAssemblyMarker.Assembly,
        WebApiAssemblyMarker.Assembly,
        LibraryEntityAbstractionsAssemblyMarker.Assembly,
        LibraryEntityFrameworkCoreAssemblyMarker.Assembly
    ];
}
