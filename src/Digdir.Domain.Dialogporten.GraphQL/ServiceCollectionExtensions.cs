using Digdir.Domain.Dialogporten.GraphQL.EndUser;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;

namespace Digdir.Domain.Dialogporten.GraphQL;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDialogportenGraphQl(this IServiceCollection services)
    {
        return services
            .AddGraphQLServer()
            // This assumes that subscriptions have been set up by the infrastructure
            .AddAuthorization()
            .AddSubscriptionType<Subscriptions>()
            .RegisterDbContext<DialogDbContext>()
            .AddDiagnosticEventListener<ApplicationInsightEventListener>()
            .AddQueryType<Queries>()
            .AddType<DialogByIdDeleted>()
            .AddType<DialogByIdNotFound>()
            .AddType<DialogByIdForbidden>()
            .AddType<SearchDialogValidationError>()
            .AddType<SearchDialogForbidden>()
            .AddInstrumentation()
            .InitializeOnStartup()
            .Services;
    }
}
