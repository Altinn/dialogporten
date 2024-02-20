﻿using System.Collections.ObjectModel;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.OrganizationRegistry;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Library.Entity.Abstractions.Features.Lookup;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using NSubstitute;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public class DialogApplication : IAsyncLifetime
{
    private Respawner _respawner = null!;
    private ServiceProvider _rootProvider = null!;
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15.4")
        .Build();

    public async Task InitializeAsync()
    {
        AssertionOptions.AssertEquivalencyUsing(options =>
        {
            //options.ExcludingMissingMembers();
            options.Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
                .WhenTypeIs<DateTimeOffset>();
            return options;
        });

        var configuration = CreateConfigurationSubstitution();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient<IOrganizationRegistry, OrganizationRegistryClient>((services, client)
            => client.BaseAddress = new Uri("https://altinncdn.no/"));

        serviceCollection.AddHttpClient<INameRegistry, NameRegistryClient>((services, client)
            => client.BaseAddress = new Uri("https://notcurrentlyinuse.no/"));

        _rootProvider = serviceCollection
            .AddApplication(configuration, Substitute.For<IHostEnvironment>())
            .AddDistributedMemoryCache()
            .AddDbContext<DialogDbContext>(x => x.UseNpgsql(_dbContainer.GetConnectionString()))
            .AddScoped<IDialogDbContext>(x => x.GetRequiredService<DialogDbContext>())
            .AddScoped<IUser, IntegrationTestUser>()
            .AddScoped<IResourceRegistry, LocalDevelopmentResourceRegistry>()
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .BuildServiceProvider();

        await _dbContainer.StartAsync();
        await EnsureDatabaseAsync();
        await BuildRespawnState();
    }

    private static IConfiguration CreateConfigurationSubstitution()
    {
        var configurationSubstitution = Substitute.For<IConfiguration>();
        var localDevSettings = new LocalDevelopmentSettings
        {
            UseLocalDevelopmentUser = true,
            UseLocalDevelopmentResourceRegister = true
        };

        configurationSubstitution
            .GetLocalDevelopmentSettings()
            .Returns(localDevSettings);

        return configurationSubstitution;
    }


    public async Task DisposeAsync()
    {
        await _rootProvider.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _rootProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        return await mediator.Send(request);
    }

    public async Task ResetState()
    {
        await using var connection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    private async Task EnsureDatabaseAsync()
    {
        using var scope = _rootProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
        await context.Database.MigrateAsync();
    }

    private async Task BuildRespawnState()
    {
        await using var connection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new()
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = new[] { new Respawn.Graph.Table("__EFMigrationsHistory") }
                .Concat(GetLookupTables())
                .ToArray()
        });
    }

    private ReadOnlyCollection<Table> GetLookupTables()
    {
        using var scope = _rootProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
        return db.Model.GetEntityTypes()
            .Where(x => typeof(ILookupEntity).IsAssignableFrom(x.ClrType))
            .Select(x => new Respawn.Graph.Table(x.GetTableName()!))
            .ToList()
            .AsReadOnly();
    }
}
