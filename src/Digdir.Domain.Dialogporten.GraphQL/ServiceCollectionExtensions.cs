using Digdir.Domain.Dialogporten.GraphQL.EndUser;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;
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
            .AddSubscriptionType<Subscriptions>()
            .AddAuthorization()
            .RegisterDbContextFactory<DialogDbContext>()
            // .RegisterDbContext<DialogDbContext>()
            .AddDiagnosticEventListener<OpenTelemetryEventListener>()
            .AddQueryType<Queries>()
            .AddMutationType<Mutations>()
            .AddType<DialogByIdDeleted>()
            .AddType<DialogByIdNotFound>()
            .AddType<DialogByIdForbidden>()
            .AddType<SearchDialogValidationError>()
            .AddType<SearchDialogForbidden>()
            .AddType<SetSystemLabelEntityNotFound>()
            .AddMaxExecutionDepthRule(12)
            .AddInstrumentation()
            .InitializeOnStartup()
            .Services;
    }
}
