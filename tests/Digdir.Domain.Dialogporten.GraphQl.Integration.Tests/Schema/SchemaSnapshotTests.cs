using Digdir.Domain.Dialogporten.GraphQL;
using HotChocolate.Execution;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Path = System.IO.Path;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.GraphQl.Integration.Tests.Schema;

public class SchemaSnapshotTests
{
    [Fact]
    public async Task FailIfGraphQlSchemaSnapshotDoesNotMatch()
    {
        // Arrange
        // This test checks for changes against the published version of the schema.verified.graphql file
        // The file is located at /docs/schema/ on the solution root
        // Commiting a change to this file will trigger a build and publish
        // of the npm package located in the same folder
        var rootPath = Utils.GetSolutionRootFolder();
        var schemaPath = Path.Combine(rootPath!, "docs/schema/V1");

        // This mock is needed for ApplicationInsightEventListener
        var telemetryConfig = new TelemetryConfiguration
        {
            TelemetryChannel = Substitute.For<ITelemetryChannel>(),
            TelemetryInitializers = { new OperationCorrelationTelemetryInitializer() }
        };

        var builder = WebApplication.CreateBuilder([]);
        builder.Services
            .AddSingleton(new TelemetryClient(telemetryConfig))
            .AddDialogportenGraphQl();

        var app = builder.Build();
        var requestExecutor =
            await app.Services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync(null, TestContext.Current.CancellationToken);

        // Act
        var schema = requestExecutor.Schema.Print();

        // Assert
        await Verify(schema, extension: "graphql")
            .UseFileName("schema")
            .UseDirectory(schemaPath);
    }
}
