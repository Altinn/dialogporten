using System.Diagnostics;
using System.Diagnostics.Metrics;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class FeatureMetricBehaviour<TRequest, TResponse>(
    IHostEnvironment hostEnvironment,
    IUser user,
    IFeatureMetricDeliveryContext deliveryContext,
    IServiceResourceResolver<TRequest> resourceResolver)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    private readonly IUser _user = user ?? throw new ArgumentNullException(nameof(user));
    private readonly IServiceResourceResolver<TRequest> _resourceResolver = resourceResolver ?? throw new ArgumentNullException(nameof(resourceResolver));
    private readonly FeatureMetricDeliveryContext _deliveryContext = deliveryContext
        as FeatureMetricDeliveryContext ?? throw new ArgumentException(
            $"Expected argument to be an instance of {nameof(FeatureMetricDeliveryContext)}",
            nameof(deliveryContext));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var environment = _hostEnvironment.EnvironmentName;
        _user.GetPrincipal().TryGetOrganizationShortName(out var performingOrg);
        var resource = await _resourceResolver.Resolve(request, cancellationToken);
        _deliveryContext.Record(new(name, environment, performingOrg, resource?.OwnOrgShortName, resource?.ResourceId));
        return await next(cancellationToken);
    }
}

internal interface IServiceResourceResolver<in TRequest>
{
    Task<ServiceResourceInformation?> Resolve(TRequest request, CancellationToken cancellationToken);
}

public interface IFeatureMetricDeliveryContext
{
    void Ack(string presentationTag);
    void Nack(string presentationTag);
    void Abandon();
}

internal sealed class FeatureMetricDeliveryContext : IFeatureMetricDeliveryContext
{
    private readonly List<FeatureMetricRecord> _records = [];

    internal void Record(FeatureMetricRecord record)
    {
        _records.Add(record);
    }

    public void Ack(string presentationTag)
    {
        // PresentationTag                                          TransactionName     Count
        // "PUT api/v1/serviceowner/dialogs/{ID}"                   Get                 55
        // "PUT api/v1/serviceowner/dialogs/{ID}"                   Update              55
        // "POST api/v1/serviceowner/dialogs/{ID}/transmissions"    Get                 105
        // "POST api/v1/serviceowner/dialogs/{ID}/transmissions"    Update              105

        var records = _records.ToArray();
        _records.Clear();
        foreach (var record in records)
        {
            var tagList = record.ToTagList();
            tagList.Add(FeatureMetricConstants.StatusTag, true);
            tagList.Add(FeatureMetricConstants.PresentationTag, true);
            Instrumentation.TransactionCounter.Add(1, tagList);
        }
    }

    public void Nack(string presentationTag)
    {
        var records = _records.ToArray();
        _records.Clear();
        foreach (var record in records)
        {
            var tagList = record.ToTagList();
            tagList.Add(FeatureMetricConstants.StatusTag, true);
            tagList.Add(FeatureMetricConstants.PresentationTag, true);
            Instrumentation.TransactionCounter.Add(1, tagList);
        }
    }

    public void Abandon() => _records.Clear();
}

internal sealed record FeatureMetricRecord(
    string TransactionName,
    string Environment,
    string? PerformerOrg = null,
    string? OwnerOrg = null,
    string? ServiceResource = null);

internal static class Instrumentation
{
    private static readonly Meter Meter = new("Digdir.Domain.Dialogporten.Application");
    public static readonly Counter<long> TransactionCounter = Meter.CreateCounter<long>(
        "cost_transactions_total",
        description: "Total number of cost management transactions processed");

    internal static TagList ToTagList(this FeatureMetricRecord record)
    {
        const string unknown = "unknown";
        var (transactionType, environment, tokenOrg, serviceOrg, serviceResource) = record;
        return new TagList
        {
            { FeatureMetricConstants.TransactionTypeTag, transactionType },
            { FeatureMetricConstants.EnvironmentTag, environment },
            { FeatureMetricConstants.TokenOrgTag, tokenOrg ?? unknown },
            { FeatureMetricConstants.ServiceOrgTag, serviceOrg ?? unknown },
            { FeatureMetricConstants.ServiceResourceTag, serviceResource ?? unknown }
        };
    }
}

internal static class FeatureMetricConstants
{
    /// <summary>
    /// The name of the counter metric for transactions
    /// </summary>
    public const string TransactionCounterName = "transactions_total";

    /// <summary>
    /// Description of the transaction counter metric
    /// </summary>
    public const string TransactionCounterDescription = "Total number of dialog transactions for cost management";

    /// <summary>
    /// Tag name for transaction type
    /// </summary>
    public const string TransactionTypeTag = "transaction_type";

    /// <summary>
    /// Tag name for organization short name from token
    /// </summary>
    public const string TokenOrgTag = "token_org";

    /// <summary>
    /// Tag name for organization short name from dialog entity
    /// </summary>
    public const string ServiceOrgTag = "service_org";

    /// <summary>
    /// Tag name for service resource type from dialog entity
    /// </summary>
    public const string ServiceResourceTag = "service_resource";

    /// <summary>
    /// Tag name for success/failure status
    /// </summary>
    public const string StatusTag = "status";

    /// <summary>
    /// Tag name for HTTP status code
    /// </summary>
    public const string HttpStatusCodeTag = "http_status_code";

    /// <summary>
    /// Tag name for environment
    /// </summary>
    public const string EnvironmentTag = "environment";

    /// <summary>
    /// Status value for successful operations (2xx)
    /// </summary>
    public const string StatusSuccess = "success";

    /// <summary>
    /// Status value for failed operations (4xx)
    /// </summary>
    public const string StatusFailed = "failed";

    /// <summary>
    /// Value used when organization or service resource cannot be determined
    /// </summary>
    public const string UnknownValue = "unknown";

    public const string PresentationTag = "presentation_tag";
}
