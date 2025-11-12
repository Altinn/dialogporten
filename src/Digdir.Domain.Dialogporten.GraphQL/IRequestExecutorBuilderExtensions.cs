using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using HotChocolate.Execution.Configuration;

namespace Digdir.Domain.Dialogporten.GraphQL;

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
