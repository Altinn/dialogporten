using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests;

[CollectionDefinition(nameof(WebApiTestCollectionFixture))]
public sealed class WebApiTestCollectionFixture : ICollectionFixture<WebApiE2EFixture>;
