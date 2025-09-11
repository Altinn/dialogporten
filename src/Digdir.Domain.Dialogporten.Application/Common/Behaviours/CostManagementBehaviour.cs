using System.Diagnostics;
using System.Diagnostics.Metrics;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class CostManagementBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IRequestOwner<TRequest>? _costManagementLatids;
    private readonly IUser _user;
    private readonly CostManagementTransaction _transaction;

    public CostManagementBehaviour(
        IHostEnvironment hostEnvironment,
        IUser user,
        ICostManagementTransaction transaction,
        IRequestOwner<TRequest>? costManagementLatids = null)
    {
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _costManagementLatids = costManagementLatids;
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _transaction = (CostManagementTransaction)transaction;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var environment = _hostEnvironment.EnvironmentName;
        _user.GetPrincipal().TryGetOrganizationShortName(out var performingOrg);
        var (serviceResource, owningOrg) = _costManagementLatids is not null
            ? await _costManagementLatids.GetOwnerInformation(request, cancellationToken)
            : (null, null);
        _transaction.Record(new(name, environment, performingOrg, owningOrg, serviceResource));
        return await next(cancellationToken);
    }
}

internal interface IRequestOwner<TRequest>
{
    Task<(string? ServiceResource, string? OwnerOrg)> GetOwnerInformation(TRequest request, CancellationToken cancellationToken);
}

public interface ICostManagementTransaction
{
    void Ack(string presentationTag);
    void Nack(string presentationTag);
    void Abandon();
}

internal sealed class CostManagementTransaction : ICostManagementTransaction
{
    private readonly List<TransactionRecord> _records = [];

    internal void Record(TransactionRecord record)
    {
        _records.Add(record);
    }

    public void Ack(string presentationTag)
    {
        // GET api/v1/serviceowner/dialogs
        // GetSoDialog
        // webApi: GET api/v1/serviceowner/dialogs
        // webApi: GetSoDialog


        // PresentationTag                                          TransactionName     Count
        // "PUT api/v1/serviceowner/dialogs/{ID}"                   Get                 55
        // "PUT api/v1/serviceowner/dialogs/{ID}"                   Update              55
        // "POST api/v1/serviceowner/dialogs/{ID}/transmissions"    Get                 105
        // "POST api/v1/serviceowner/dialogs/{ID}/transmissions"    Update              105


        // GQL: GetSoDialog
        var records = _records.ToArray();
        _records.Clear();
        foreach (var record in records)
        {
            var tags = BuildMetricTags(record, success: true);
            Instrumentation.TransactionCounter.Add(1, tags);
        }
        // Todo: write success to metric here...
    }

    public void Nack(string presentationTag)
    {
        var records = _records.ToArray();
        _records.Clear();
        foreach (var record in records)
        {
            var tags = BuildMetricTags(record, success: false);
            Instrumentation.TransactionCounter.Add(1, tags);
        }
    }

    public void Abandon() => _records.Clear();

    private static TagList BuildMetricTags(TransactionRecord record, bool success)
    {
        var (transactionType, environment, tokenOrg, serviceOrg, serviceResource) = record;

        return new TagList
        {
            { CostManagementConstants.TransactionTypeTag, transactionType },
            { CostManagementConstants.StatusTag, success ? "success" : "failure" },
            { CostManagementConstants.EnvironmentTag, environment },
            { CostManagementConstants.TokenOrgTag, NormalizeTag(tokenOrg) },
            { CostManagementConstants.ServiceOrgTag, NormalizeTag(serviceOrg) },
            { CostManagementConstants.ServiceResourceTag, NormalizeTag(serviceResource) }
        };
    }

    private static string NormalizeTag(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value;
}

internal static class Instrumentation
{
    public const string Source = "Digdir.Domain.Dialogporten.Application";
    public const string Name = "CostManagement";
    public const string Version = "1.0.0";
    public static readonly Meter Meter = new(Source, Version);
    public static readonly Counter<long> TransactionCounter = Meter.CreateCounter<long>(
        "cost_transactions_total",
        description: "Total number of cost management transactions processed");
}

public readonly record struct TransactionRecord(
    string TransactionName,
    string Environment,
    string? PerformerOrg = null,
    string? OwnerOrg = null,
    string? ServiceResource = null);

