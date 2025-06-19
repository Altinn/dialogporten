using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Infrastructure.GraphQL;

/// <summary>
/// This implementation is a workaround to allow the use of the AddRedisSubscriptions extension method
/// from HotChocolate.Subscriptions.Redis without having to take the entire HotChocolate library as a dependency.
/// </summary>
internal sealed class DummyRequestExecutorBuilder : IRequestExecutorBuilder
{
    public string Name => string.Empty;
    public IServiceCollection Services { get; init; } = null!;
}
