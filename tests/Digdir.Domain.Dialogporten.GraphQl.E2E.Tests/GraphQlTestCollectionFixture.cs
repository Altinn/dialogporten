using Xunit;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests;

[CollectionDefinition(nameof(GraphQlTestCollectionFixture))]
public sealed class GraphQlTestCollectionFixture : ICollectionFixture<GraphQlE2EFixture>;
