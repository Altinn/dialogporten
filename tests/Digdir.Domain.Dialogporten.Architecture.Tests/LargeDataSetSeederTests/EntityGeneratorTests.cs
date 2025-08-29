using System.Reflection;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.IdempotentNotifications;
using Digdir.Library.Entity.Abstractions.Features.Lookup;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ActorName = Digdir.Domain.Dialogporten.Domain.Actors.ActorName;

namespace Digdir.Domain.Dialogporten.Architecture.Tests.LargeDataSetSeederTests;

public class EntityGeneratorTests
{
    [Fact]
    public void All_Database_Entities_Should_Have_One_Matching_Generator()
    {
        var (missingGenerators, missingPropertiesInGeneratorType) = MissingGeneratorsAndProperties();

        missingGenerators.Should().BeEmpty(
            "every db table should have a corresponding generator" +
            string.Join(", ", missingGenerators));

        missingPropertiesInGeneratorType.Should().BeEmpty(
            "all db columns should be represented as a property on the corresponding generator type:\n" +
            string.Join("\n\n", missingPropertiesInGeneratorType
                .Select(kv => $"{kv.Key}:\n  {string.Join("\n  ", kv.Value)}")));
    }

    private static (List<string> missingGenerators, Dictionary<string, List<string>> missingPropertiesByType) MissingGeneratorsAndProperties()
    {
        var dbContext = CreateDbContext();
        var entityGenerators = GetEntityGenerators();

        List<string> missingGenerators = [];
        Dictionary<string, List<string>> missingPropertiesByType = [];

        var ents = GetEntityTypes(dbContext)
            .GroupBy(x => x.GetTableName()!)
            .ToDictionary(x => x.Key, x => x.SelectMany(x =>
            {
                var scalarColumns = x.GetProperties().Select(x => x.GetColumnName());
                var fkColumns = x.GetForeignKeys().SelectMany(x => x.Properties.Select(y => y.GetColumnName()));
                return scalarColumns.Concat(fkColumns).Distinct();
            }).ToHashSet());

        foreach (var (tableName, columns) in ents)
        {
            if (!entityGenerators.TryGetValue(tableName!, out var generatorType))
            {
                missingGenerators.Add(tableName);
                continue;
            }

            List<string> missingPropertyNames = [];

            var generatorProperties = GetGeneratorProperties(generatorType);

            foreach (var column in columns)
            {
                if (!generatorProperties.Contains(column))
                {
                    missingPropertyNames.Add(column);
                }
            }

            if (missingPropertyNames.Count != 0)
            {
                missingPropertiesByType[tableName!] = missingPropertyNames;
            }
        }

        return (missingGenerators, missingPropertiesByType);
    }

    private static HashSet<string> GetGeneratorProperties(TypeInfo generatorType) =>
        generatorType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is { CanRead: true, CanWrite: true })
            .Select(p => p.Name)
            .ToHashSet();

    private static DialogDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<DialogDbContext>()
            .UseInMemoryDatabase("TestDb").Options);

    private static Dictionary<string, TypeInfo> GetEntityGenerators() =>
        LargeDataSetSeederAssemblyMarker.Assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(IsEntityGeneratorInterface)
                .Select(iface => (Type: t, Interface: iface)))
            .GroupBy(t => t.Type.Name)
            .Select(g =>
            {
                g.Count().Should().Be(1, "Cannot have multiple generators with same name: " + g.Key);
                return g.Single();
            })
            .ToDictionary(
                t => t.Type.Name,
                t => t.Type);

    private static readonly string[] IgnoredEntityNames =
    {
        nameof(ActorName), // Handled by custom generator, runs before others
        nameof(DialogApiAction), // Don't care about these in YT01 seeding
        nameof(DialogApiActionEndpoint), // Don't care about these in YT01 seeding
        nameof(MassTransit),
        nameof(NotificationAcknowledgement),
        nameof(SubjectResource),
        nameof(ResourcePolicyInformation)
    };

    private static IEnumerable<IEntityType> GetEntityTypes(DbContext dbContext) =>
        dbContext.Model
            .GetEntityTypes()
            .Where(x => !IgnoredEntityNames.Any(ignore => x.Name.Contains(ignore)))
            .Where(x => !x.ClrType.GetInterfaces()
                .Any(i => i.IsAssignableTo(typeof(ILookupEntity))));

    private static bool IsEntityGeneratorInterface(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEntityGenerator<>);
}
