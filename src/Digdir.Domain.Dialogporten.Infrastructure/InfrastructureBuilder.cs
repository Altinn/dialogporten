using System.Reflection;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.OptionExtensions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Digdir.Domain.Dialogporten.Infrastructure.InfrastructureExtensions;

namespace Digdir.Domain.Dialogporten.Infrastructure;

public interface IPubSubInfrastructureChoice
{
    ISubscriptionInfrastructureOptions WithPubSubCapabilities<TConsumerAssembly>() => WithPubSubCapabilities(typeof(TConsumerAssembly).Assembly);
    ISubscriptionInfrastructureOptions WithPubSubCapabilities(params Assembly[] consumerAssemblies);
    IInfrastructureBuilder WithoutPubSubCapabilities();
    IInfrastructureBuilder WithPubCapabilities();
}

public interface ISubscriptionInfrastructureOptions : IInfrastructureBuilder
{
    IInfrastructureBuilder AndBusConfiguration(Action<IBusRegistrationConfigurator> configure);
}

public interface IInfrastructureBuilder
{
    IServiceCollection Build();
}

internal sealed class InfrastructureBuilder(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) :
    IInfrastructureBuilder,
    IPubSubInfrastructureChoice,
    ISubscriptionInfrastructureOptions
{
    private readonly IServiceCollection _services = services ?? throw new ArgumentNullException(nameof(services));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly List<Action<InfrastructureBuilderContext>> _actions = [];
    private readonly List<Action<IBusRegistrationConfigurator>> _busConfigurations = [];

    public IServiceCollection Build()
    {
        var infrastructureSettings = RegisterAndValidateInfrastructureSettings();
        var developmentSettings = _configuration.GetLocalDevelopmentSettings();
        var builderContext = new InfrastructureBuilderContext(
            _services,
            _configuration,
            _environment,
            infrastructureSettings,
            developmentSettings);
        AddInfrastructure_Internal(builderContext);
        foreach (var action in _actions)
        {
            action(builderContext);
        }
        return _services;
    }

    public IInfrastructureBuilder AndBusConfiguration(Action<IBusRegistrationConfigurator> configure)
    {
        _busConfigurations.Add(configure);
        return this;
    }

    public ISubscriptionInfrastructureOptions WithPubSubCapabilities(params Assembly[] consumerAssemblies)
    {
        _busConfigurations.Add(x => x
            .AddConsumers(consumerAssemblies
            .DefaultIfEmpty(Assembly.GetEntryAssembly())
            .ToArray()));
        return AddAction(context => AddPubSubCapabilities(context, _busConfigurations));
    }

    public IInfrastructureBuilder WithPubCapabilities() => AddAction(AddPubCapabilities);
    public IInfrastructureBuilder WithoutPubSubCapabilities() => this;

    private InfrastructureBuilder AddAction(Action<InfrastructureBuilderContext> action)
    {
        _actions.Add(action);
        return this;
    }

    private InfrastructureSettings RegisterAndValidateInfrastructureSettings()
    {
        var infrastructureConfigurationSection = configuration
            .GetSection(InfrastructureSettings.ConfigurationSectionName);
        services.AddOptions<InfrastructureSettings>()
            .Bind(infrastructureConfigurationSection)
            .ValidateFluently();
        var settings = infrastructureConfigurationSection.Get<InfrastructureSettings>()
                       ?? throw new InvalidOperationException("Infrastructure settings are not configured.");
        var validator = new FluentValidationOptions<InfrastructureSettings>(InfrastructureAssemblyMarker.Assembly);
        var validatorResult = validator.Validate(null, settings);
        return validatorResult.Failed
            ? throw new InvalidOperationException($"Infrastructure settings are not valid.{Environment.NewLine}{validatorResult.FailureMessage}")
            : settings;
    }
}

internal sealed record InfrastructureBuilderContext(
    IServiceCollection Services,
    IConfiguration Configuration,
    IHostEnvironment Environment,
    InfrastructureSettings InfraSettings,
    LocalDevelopmentSettings DevSettings);
