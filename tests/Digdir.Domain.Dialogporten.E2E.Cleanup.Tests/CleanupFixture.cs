using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Library.Dialogporten.E2E.Common;
using Xunit;

namespace Digdir.Domain.Dialogporten.E2E.Cleanup.Tests;

public sealed class CleanupFixture : E2EFixtureBase
{
    protected override bool IncludeGraphQlPreflight => false;

    // Scheduled by GitHub Actions at 04:00 UTC (dispatch-purge-e2e-test-data.yml)
    // and can be run manually in GitHub Actions → “Purge E2E test data” with a selected environment/ref.
    public void PurgeE2ETestDialogs()
    {
        using var _ = UseServiceOwnerTokenOverrides(
            scopes: E2EConstants.ServiceOwnerScopes + " "
                    + AuthorizationScope.ServiceOwnerAdminScope);

        var cancellationToken = TestContext.Current.CancellationToken;
        var queryParams = new V1ServiceOwnerDialogsQueriesSearchDialogQueryParams
        {
            ServiceOwnerLabels = [E2EConstants.EphemeralDialogUrn],
            Limit = 1000
        };

        PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog? page;
        do
        {
            var searchResult = ServiceownerApi
                .V1ServiceOwnerDialogsQueriesSearchDialog(
                    queryParams, cancellationToken)
                .GetAwaiter()
                .GetResult();

            if (!searchResult.IsSuccessful || searchResult.Content is null)
            {
                TestContext.Current?.AddWarning(
                    "Failed to search dialogs for cleanup: " +
                    $"{searchResult.Error?.Message ?? "unknown error"}");
                return;
            }

            page = searchResult.Content;
            foreach (var dialog in page.Items ?? [])
            {
                try
                {
                    var purgeResult = ServiceownerApi
                        .V1ServiceOwnerDialogsCommandsPurgeDialog(
                            dialog.Id, if_Match: null, cancellationToken)
                        .GetAwaiter()
                        .GetResult();

                    if (!purgeResult.IsSuccessful)
                    {
                        TestContext.Current?.AddWarning(
                            $"Failed to delete dialog {dialog.Id}: " +
                            $"{purgeResult.Error?.Message ?? "unknown error"}");
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Current?.AddWarning(
                        $"Failed to delete dialog {dialog.Id}: " +
                        $"{exception.GetBaseException().Message}");
                }
            }

            if (page.HasNextPage)
            {
                queryParams.ContinuationToken = new()
                {
                    AdditionalProperties = new Dictionary<string, object>
                    {
                        ["continuationToken"] = page.ContinuationToken
                    }
                };
            }
        } while (page.HasNextPage);
    }
}
