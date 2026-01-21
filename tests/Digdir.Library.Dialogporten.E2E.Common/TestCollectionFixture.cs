using Xunit;

namespace Digdir.Library.Dialogporten.E2E.Common;

[CollectionDefinition(nameof(TestCollectionFixture))]
public sealed class TestCollectionFixture : ICollectionFixture<E2EFixture>;
