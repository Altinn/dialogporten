using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class AzureStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureStorageService> _logger;
    private readonly MetricsAggregationOptions _options;

    public AzureStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<AzureStorageService> logger,
        IOptions<MetricsAggregationOptions> options)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task UploadParquetFileAsync(byte[] parquetData, string fileName, CancellationToken cancellationToken = default)
    {

        _logger.LogInformation("Uploading Parquet file {FileName} ({FileSize} bytes) to Azure Storage",
            fileName, parquetData.Length);

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.StorageContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = new MemoryStream(parquetData);
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully uploaded {FileName} to Azure Storage container {ContainerName}",
                fileName, _options.StorageContainerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload {FileName} to Azure Storage", fileName);
            throw;
        }
    }
}
