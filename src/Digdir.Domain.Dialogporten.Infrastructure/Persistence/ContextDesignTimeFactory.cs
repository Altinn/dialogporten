using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence;

internal sealed class ContextDesignTimeFactory : IDesignTimeDbContextFactory<DialogDbContext>
{
    private const string ConnectionStringConfigName = "Infrastructure:DialogDbConnectionString";

    public DialogDbContext CreateDbContext(string[] args)
    {
        var localPostgresConnectionString = new ConfigurationBuilder()
            .AddUserSecrets(InfrastructureAssemblyMarker.Assembly, true)
            .Build()[ConnectionStringConfigName];

        return new(new DbContextOptionsBuilder<DialogDbContext>()
            .UseNpgsql(localPostgresConnectionString)
            .Options);
    }
}
