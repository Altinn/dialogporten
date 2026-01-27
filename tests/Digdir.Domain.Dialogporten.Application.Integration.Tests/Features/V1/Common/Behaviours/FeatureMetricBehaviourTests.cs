using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Behaviours;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class FeatureMetricBehaviourTests : ApplicationCollectionFixture
{
    public FeatureMetricBehaviourTests(DialogApplication application) : base(application) { }

    private async Task<List<FeatureMetricRecord>> ExecuteCommandAndGetMetrics<T>(IRequest<T> command)
    {
        using var scope = Application.GetServiceProvider().CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<FeatureMetricRecorder>();

        // Execute command directly using the scoped provider
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        await mediator.Send(command);

        // Return the metrics from the same scope
        return recorder.Records.ToList();
    }

    [Fact]
    public async Task CreateDialog_Should_Include_All_Required_Fields()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var command = new CreateDialogCommand
        {
            Dto = new CreateDialogDto
            {
                Id = expectedDialogId,
                ServiceResource = "urn:altinn:resource:test-service",
                Party = "urn:altinn:organization:identifier-no:912345678",
                Content = new Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ContentDto
                {
                    Title = new ContentValueDto
                    {
                        Value = [new LocalizationDto { LanguageCode = "nb", Value = "Test Dialog" }],
                        MediaType = "text/plain"
                    }
                }
            }
        };

        // Act
        var metrics = await ExecuteCommandAndGetMetrics(command);

        // Assert
        var metric = Assert.Single(metrics);
        Assert.Equal(typeof(CreateDialogCommand).FullName, metric.FeatureName);
        Assert.False(string.IsNullOrEmpty(metric.Environment));
        Assert.False(string.IsNullOrEmpty(metric.CallerOrg));
        Assert.NotEqual("unknown", metric.CallerOrg);
        Assert.False(string.IsNullOrEmpty(metric.OwnerOrg));
        Assert.NotEqual("unknown", metric.OwnerOrg);
        Assert.False(string.IsNullOrEmpty(metric.ServiceResource));
    }

    [Fact]
    public async Task FeatureMetric_ShouldRecordMetricsForMultipleOperations()
    {
        // Arrange
        var firstDialogId = IdentifiableExtensions.CreateVersion7();
        var secondDialogId = IdentifiableExtensions.CreateVersion7();

        var createCommand1 = new CreateDialogCommand
        {
            Dto = new CreateDialogDto
            {
                Id = firstDialogId,
                ServiceResource = "urn:altinn:resource:test-service-1",
                Content = new Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ContentDto
                {
                    Title = new ContentValueDto
                    {
                        Value = [new LocalizationDto { LanguageCode = "nb", Value = "Test Dialog 1" }],
                        MediaType = "text/plain"
                    }
                }
            }
        };

        var createCommand2 = new CreateDialogCommand
        {
            Dto = new CreateDialogDto
            {
                Id = secondDialogId,
                ServiceResource = "urn:altinn:resource:test-service-2",
                Content = new Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ContentDto
                {
                    Title = new ContentValueDto
                    {
                        Value = [new LocalizationDto { LanguageCode = "nb", Value = "Test Dialog 2" }],
                        MediaType = "text/plain"
                    }
                }
            }
        };

        // Act - Execute all commands in the same scope
        using var scope = Application.GetServiceProvider().CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<FeatureMetricRecorder>();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        await mediator.Send(createCommand1, TestContext.Current.CancellationToken);

        var getCommand1 = new GetDialogQuery { DialogId = firstDialogId };
        await mediator.Send(getCommand1, TestContext.Current.CancellationToken);

        await mediator.Send(createCommand2, TestContext.Current.CancellationToken);

        var getCommand2 = new GetDialogQuery { DialogId = secondDialogId };
        await mediator.Send(getCommand2, TestContext.Current.CancellationToken);

        // Assert
        var metrics = recorder.Records.ToList();
        Assert.True(metrics.Count == 4); // 2x CreateDialog + 2x GetDialog

        // All metrics should have valid service resources and owner orgs
        var serviceResources = metrics.Select(m => m.ServiceResource).ToList();
        Assert.All(serviceResources, sr => Assert.False(string.IsNullOrEmpty(sr)));

        var ownerOrgs = metrics.Select(m => m.OwnerOrg).ToList();
        Assert.All(ownerOrgs, owner => Assert.False(string.IsNullOrEmpty(owner)));

        // Different dialogs should have different service resources
        var createDialogMetrics = metrics.Where(m => m.FeatureName.Contains("CreateDialog")).ToList();
        Assert.True(createDialogMetrics.Count == 2);

        var getDialogMetrics = metrics.Where(m => m.FeatureName.Contains("GetDialog")).ToList();
        Assert.True(getDialogMetrics.Count == 2);
    }
}
