using System.Reflection;
using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;

public class WarmupService : IHostedService
{
    // Should exist in all environments
    private const string PartyId = "urn:altinn:organization:identifier-no:974760673";
    private const string ServiceOwnerOrgNo = "991825827";
    private const string EndUserPid = "14886498226";
    private const string ServiceResource = "urn:altinn:resource:ttd-dialogporten-automated-tests";

    private const string WebApiRuntimeAssembly = "Digdir.Domain.Dialogporten.WebApi";
    private const string GraphQlRuntimeAssembly = "Digdir.Domain.Dialogporten.GraphQL";

    private readonly ILogger<WarmupService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WarmupState _warmupState;
    private CancellationTokenSource? _internalCts;

    public WarmupService(
        ILogger<WarmupService> logger,
        IServiceScopeFactory scopeFactory,
        WarmupState warmupState)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _warmupState = warmupState;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Application Warmup: Queuing background warmup task...");

        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var warmupToken = _internalCts.Token;

        _ = Task.Run(() => PerformWarmupAsync(warmupToken), warmupToken);

        // Return immediately so the host startup is not blocked so that health endpoints can be called
        return Task.CompletedTask;
    }

    private async Task PerformWarmupAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Warmup: Background task execution started.");

        using var scope = _scopeFactory.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        try
        {
            var sender = scopedProvider.GetRequiredService<ISender>();

            CreateServiceOwnerHttpContext(scopedProvider);
            await PerformServiceOwnerRequests(sender, cancellationToken);

            CreateEndUserHttpContext(scopedProvider);
            await PerformEndUserRequests(sender, cancellationToken);

            /*
            // Wait for Kestrel to initialize and then perform local HTTP requests
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            await PerformLocalHttpRequests(cancellationToken);
            */

            _logger.LogInformation("Warmup: Background task completed successfully.");
            _warmupState.MarkWarmupComplete();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Warmup: Background task was cancelled.");
            _warmupState.MarkWarmupFailed(new OperationCanceledException("Warmup cancelled.")); // Mark as failed if cancelled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warmup: Background task failed unexpectedly.");
            _warmupState.MarkWarmupFailed(ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Warmup: Stopping.");
        return Task.CompletedTask;
    }

    private static async Task PerformServiceOwnerRequests(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(GetCreateDialogCommand(), cancellationToken);
        if (result.IsT0)
        {
            await sender.Send(new PurgeDialogCommand()
            {
                IsSilentUpdate = true,
                DialogId = result.AsT0.DialogId,
            }, cancellationToken);
        }
    }

    private static async Task PerformEndUserRequests(ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(GetEnduserSearchDialogQuery(), cancellationToken);
    }

    private static void CreateEndUserHttpContext(IServiceProvider scopedProvider)
    {
        CreateHttpContext(scopedProvider, new List<Claim>
        {
            new("scope", "digdir:dialogporten"),
            new("pid", EndUserPid)
        });
    }

    private static void CreateServiceOwnerHttpContext(IServiceProvider scopedProvider)
    {
        CreateHttpContext(scopedProvider, new List<Claim>
        {
            new("scope", "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.admin"),
            new("consumer", $"{{\"authority\":\"iso6523-actorid-upis\",\"ID\":\"0192:{ServiceOwnerOrgNo}\"}}")
        });
    }

    private static void CreateHttpContext(IServiceProvider scopedProvider, List<Claim> claims)
    {
        var httpContextAccessor = scopedProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scopedProvider
        };

        var identity = new ClaimsIdentity(claims, "WarmupAuth");
        httpContext.User = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext = httpContext;
    }

    private static async Task PerformLocalHttpRequests(CancellationToken cancellationToken)
    {
        var localKestrelAddress = GetLocalKestrelAdress();
        if (localKestrelAddress is null)
        {
            return;
        }

        // Attempt to perform requests to both the WebAPI as well as the GraphQL endpoints
        // to preload JWKs from issuer well-knowns and get som JIT-ing done
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
            // Random invalid token
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30");
        if (assemblyName == WebApiRuntimeAssembly)
        {
            var response = await httpClient.GetAsync(localKestrelAddress + $"api/v1/enduser/dialogs?Party={PartyId}", cancellationToken: cancellationToken);
            Console.WriteLine(response.ReasonPhrase);
        }
        else if (assemblyName == GraphQlRuntimeAssembly)
        {
            // Perform GraphQL warmup requests here
        }
        // Don't bother with other runtimes (service, janitor)
    }

    private static Uri? GetLocalKestrelAdress()
    {
        var urlsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (urlsEnv is null) return null;
        var urls = urlsEnv.Split(';');
        foreach (var urlString in urls)
        {
            // Replace + or * with localhost for Uri.TryCreate to parse correctly for host
            var parsableUrlString = urlString.Replace("*", "localhost").Replace("+", "localhost");
            if (Uri.TryCreate(parsableUrlString, UriKind.Absolute, out var uri))
            {
                return uri;
            }
        }
        return null;
    }

    private static CreateDialogCommand GetCreateDialogCommand()
    {
        return new CreateDialogCommand
        {
            IsSilentUpdate = true,
            Dto = new CreateDialogDto
            {
                VisibleFrom = DateTimeOffset.MaxValue,
                Status = DialogStatus.Values.New,
                Party = PartyId,
                ServiceResource = ServiceResource,
                IsApiOnly = true,
                Activities = [
                    new ActivityDto
                    {
                        Type = DialogActivityType.Values.DialogCreated,
                        PerformedBy = new ActorDto
                        {
                            ActorId = PartyId,
                            ActorType = ActorType.Values.PartyRepresentative
                        }
                    }
                ]
            }
        };
    }
    private static SearchDialogQuery GetEnduserSearchDialogQuery()
    {
        return new SearchDialogQuery
        {
            Party = [PartyId],
            Limit = 1
        };
    }
}

public class WarmupState
{
    public bool IsWarmupComplete { get; private set; }
    private readonly TaskCompletionSource _warmupTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void MarkWarmupComplete()
    {
        IsWarmupComplete = true;
        _warmupTcs.TrySetResult();
    }

    public Task WaitForWarmupAsync(CancellationToken cancellationToken)
    {
        return _warmupTcs.Task.WaitAsync(cancellationToken);
    }

    public void MarkWarmupFailed(Exception ex)
    {
        IsWarmupComplete = false;
        _warmupTcs.TrySetException(ex);
    }
}
