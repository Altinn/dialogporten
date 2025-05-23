using AppAny.HotChocolate.FluentValidation;
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
            .ModifyCostOptions(o => o.ApplyCostDefaults = false)
            // This assumes that subscriptions have been set up by the infrastructure
            .AddSubscriptionType<Subscriptions>()
            .AddAuthorization()
            .RegisterDbContextFactory<DialogDbContext>()
            .AddFluentValidation()
            .AddQueryType<Queries>()
            .AddMutationType<Mutations>()
            .AddType<DialogByIdDeleted>()
            .AddType<DialogByIdNotFound>()
            .AddType<DialogByIdForbidden>()
            .AddType<DialogByIdForbiddenAuthLevelTooLow>()
            .AddType<SearchDialogValidationError>()
            .AddType<SearchDialogForbidden>()
            .AddType<SetSystemLabelEntityNotFound>()
            .AddType<BulkSetSystemLabelForbidden>()
            .AddType<BulkSetSystemLabelDomainError>()
            .AddType<BulkSetSystemLabelValidationError>()
            .AddType<BulkSetSystemLabelConcurrencyError>()
            .AddType<SearchDialogOrderByParsingError>()
            .AddType<SearchDialogContinuationTokenParsingError>()
            .AddMaxExecutionDepthRule(12)
            .AddInstrumentation()
            .InitializeOnStartup()
            .Services;
    }
}
