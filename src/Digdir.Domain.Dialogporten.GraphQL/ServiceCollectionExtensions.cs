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
        .AddGraphQLServer()
        .BindRuntimeType<Uri, UrlType>()
        .AddHttpRequestInterceptor<DialogportenHttpRequestInterceptor>()
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
        // In HotChocolate 16 the root span is owned by ASP.NET Core, so we capture the operation name
        // here and apply it to the request activity in Program.cs (EnrichWithHttpResponse).
        .AddDiagnosticEventListener<RenameRootActivityListener>()
        .Services;
}
