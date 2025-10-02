using AppAny.HotChocolate.FluentValidation;
using Digdir.Domain.Dialogporten.GraphQL.EndUser;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using HotChocolate.Execution.Configuration;

namespace Digdir.Domain.Dialogporten.GraphQL;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDialogportenGraphQl(this IServiceCollection services) => services
        .AddGraphQLServer()
        .ModifyCostOptions(o => o.ApplyCostDefaults = false)
        // This assumes that subscriptions have been set up by the infrastructure
        .AddSubscriptionType<Subscriptions>()
        .AddAuthorization()
        .RegisterDbContextFactory<DialogDbContext>()
        .AddFluentValidation()
        .AddQueryType<Queries>()
        .AddMutationType<Mutations>()
        .AddErrorTypes()
        .AddMaxExecutionDepthRule(12)
        .AddInstrumentation()
        .InitializeOnStartup()
        .Services;
}

internal static class IRequestExecutorBuilderExtensions
{
    internal static IRequestExecutorBuilder AddErrorTypes(this IRequestExecutorBuilder builder)
    {
        var errorInterfaces = new[]
        {
            typeof(IBulkSetSystemLabelError),
            typeof(ISetSystemLabelError),
            typeof(ISearchDialogError),
            typeof(IDialogByIdError)
        };

        var errorTypes = typeof(ServiceCollectionExtensions).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && errorInterfaces
                .Any(i => i.IsAssignableFrom(t)) && t.IsClass)
            .ToList();

        return errorTypes.Aggregate(builder, (current, type) => current.AddType(type));
    }
}
