using Digdir.Domain.Dialogporten.GraphQL.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using HotChocolate.Diagnostics;

namespace Digdir.Domain.Dialogporten.GraphQL;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDialogportenGraphQl(this IServiceCollection services) => services
        .AddTransient<ActivityEnricher, DialogportenGqlActivityEnricher>()
        .AddGraphQLServer()
        .BindRuntimeType<Uri, UrlType>()
        .AddHttpRequestInterceptor<DialogportenHttpRequestInterceptor>()
        .TryAddTypeInterceptor<EnableResponseCompressionTypeInterceptor>()
        .ModifyCostOptions(o => o.ApplyCostDefaults = false)
        // This assumes that subscriptions have been set up by the infrastructure
        .AddSubscriptionType<Subscriptions>()
        .AddAuthorization()
        .RegisterDbContextFactory<DialogDbContext>()
        .AddQueryType<Queries>()
        .AddMutationType<Mutations>()
        .AddErrorTypes()
        .AddMaxExecutionDepthRule(12)
        .AddInstrumentation()
        .Services;
}
