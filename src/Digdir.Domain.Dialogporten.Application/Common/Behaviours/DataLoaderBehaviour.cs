using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class DataLoaderBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IRequireData
{
    private readonly IEnumerable<IDataLoader<TRequest, TResponse>> _loaders;

    public DataLoaderBehaviour(IEnumerable<IDataLoader<TRequest, TResponse>> loaders)
    {
        _loaders = loaders;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        foreach (var loader in _loaders)
        {
            await loader.Load(request, cancellationToken);
        }

        return await next(cancellationToken);
    }
}

public interface IRequireData
{
    Dictionary<string, object?> Data { get; set; }
}

internal static class IRequireDataExtensions
{
    internal static T? GetRequiredData<T>(this IRequireData request, string key)
        => (T?)request.Data[key];
}

public interface IDataLoader<in TRequest, TResponse> where TRequest : IRequest<TResponse>, IRequireData
{
    Task Load(TRequest request, CancellationToken cancellationToken);
}

internal static class IDataLoaderExtensions
{
    internal static IServiceCollection AddDataLoaders(this IServiceCollection services, params Assembly[] assemblies)
    {
        var loaderTypes = assemblies
            .DefaultIfEmpty(Assembly.GetCallingAssembly())
            .SelectMany(a => a.DefinedTypes)
            .Where(t => !t.ContainsGenericParameters)
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t =>
                t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataLoader<,>))
                    .Select(i => new { Implementation = t, Service = i }))
            .ToList();

        foreach (var loader in loaderTypes)
        {
            services.AddTransient(loader.Service, loader.Implementation);
        }

        return services;
    }
}

public sealed class UpdateDialogDataLoader : IDataLoader<UpdateDialogCommand, UpdateDialogResult>
{
    private readonly IDialogDbContext _dialogDbContext;
    private readonly IUserResourceRegistry _userResourceRegistry;
    public const string Key = nameof(UpdateDialogDataLoader);

    public UpdateDialogDataLoader(IDialogDbContext dialogDbContext, IUserResourceRegistry userResourceRegistry)
    {
        _dialogDbContext = dialogDbContext;
        _userResourceRegistry = userResourceRegistry;
    }

    public async Task Load(UpdateDialogCommand request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);
        if (resourceIds.Count == 0)
        {
            request.Data[Key] = null;
            return;
        }

        var dialog = await _dialogDbContext.Dialogs
            .Include(x => x.Activities)
            .Include(x => x.Content)
                .ThenInclude(x => x.Value.Localizations)
            .Include(x => x.SearchTags)
            .Include(x => x.Attachments)
                .ThenInclude(x => x.DisplayName!.Localizations)
            .Include(x => x.Attachments)
                .ThenInclude(x => x.Urls)
            .Include(x => x.GuiActions)
                .ThenInclude(x => x.Title!.Localizations)
            .Include(x => x.GuiActions)
                .ThenInclude(x => x.Prompt!.Localizations)
            .Include(x => x.ApiActions)
                .ThenInclude(x => x.Endpoints)
            .Include(x => x.Transmissions)
            .Include(x => x.DialogEndUserContext)
            .IgnoreQueryFilters()
            .WhereIf(!_userResourceRegistry.IsCurrentUserServiceOwnerAdmin(), x => resourceIds.Contains(x.ServiceResource))
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        request.Data[Key] = dialog;
    }
}
