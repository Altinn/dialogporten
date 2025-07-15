using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.ValueConverters;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Library.Entity.Abstractions.Features.Versionable;
using Digdir.Library.Entity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.IdempotentNotifications;
using EntityFramework.Exceptions.PostgreSQL;
using MassTransit;
using FluentAssertions;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Tests;

public class DialogDbContextTests : IDisposable
{
    private readonly DialogDbContext _context;
    private readonly DbContextOptions<DialogDbContext> _options;

    public DialogDbContextTests()
    {
        _options = new DbContextOptionsBuilder<DialogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new DialogDbContext(_options);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldInitializeContext()
    {
        // Arrange & Act
        var context = new DialogDbContext(_options);

        // Assert
        context.Should().NotBeNull();
        context.Should().BeAssignableTo<DbContext>();
        context.Should().BeAssignableTo<IDialogDbContext>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new DialogDbContext(null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DbSetProperties_ShouldReturnValidDbSets()
    {
        // Assert - Test all DbSet properties
        _context.Dialogs.Should().NotBeNull();
        _context.DialogStatuses.Should().NotBeNull();
        _context.DialogActivities.Should().NotBeNull();
        _context.DialogActivityTypes.Should().NotBeNull();
        _context.DialogTransmissions.Should().NotBeNull();
        _context.DialogTransmissionTypes.Should().NotBeNull();
        _context.DialogTransmissionContents.Should().NotBeNull();
        _context.DialogTransmissionContentTypes.Should().NotBeNull();
        _context.DialogApiActions.Should().NotBeNull();
        _context.DialogApiActionEndpoints.Should().NotBeNull();
        _context.DialogGuiActions.Should().NotBeNull();
        _context.DialogGuiActionPriority.Should().NotBeNull();
        _context.DialogSeenLog.Should().NotBeNull();
        _context.DialogUserTypes.Should().NotBeNull();
        _context.DialogSearchTags.Should().NotBeNull();
        _context.DialogContents.Should().NotBeNull();
        _context.DialogContentTypes.Should().NotBeNull();
        _context.SubjectResources.Should().NotBeNull();
        _context.DialogEndUserContexts.Should().NotBeNull();
        _context.DialogServiceOwnerContexts.Should().NotBeNull();
        _context.DialogServiceOwnerLabels.Should().NotBeNull();
        _context.LabelAssignmentLogs.Should().NotBeNull();
        _context.NotificationAcknowledgements.Should().NotBeNull();
        _context.ResourcePolicyInformation.Should().NotBeNull();
        _context.ActorName.Should().NotBeNull();
        _context.DialogAttachments.Should().NotBeNull();
    }

    [Fact]
    public void TrySetOriginalRevision_WithNullEntity_ShouldReturnFalse()
    {
        // Arrange
        TestVersionableEntity entity = null;
        var revision = Guid.NewGuid();

        // Act
        var result = _context.TrySetOriginalRevision(entity, revision);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetOriginalRevision_WithNullRevision_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestVersionableEntity { Id = Guid.NewGuid() };
        Guid? revision = null;

        // Act
        var result = _context.TrySetOriginalRevision(entity, revision);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetOriginalRevision_WithDefaultRevision_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestVersionableEntity { Id = Guid.NewGuid() };
        Guid? revision = default(Guid);

        // Act
        var result = _context.TrySetOriginalRevision(entity, revision);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TrySetOriginalRevision_WithValidEntityAndRevision_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestVersionableEntity { Id = Guid.NewGuid(), Revision = Guid.NewGuid() };
        var revision = Guid.NewGuid();
        
        // Add entity to context to track it
        _context.Entry(entity).State = EntityState.Added;

        // Act
        var result = _context.TrySetOriginalRevision(entity, revision);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TrySetOriginalRevision_WithValidEntityAndRevision_ShouldSetOriginalValue()
    {
        // Arrange
        var entity = new TestVersionableEntity { Id = Guid.NewGuid(), Revision = Guid.NewGuid() };
        var revision = Guid.NewGuid();
        
        // Add entity to context to track it
        _context.Entry(entity).State = EntityState.Added;

        // Act
        _context.TrySetOriginalRevision(entity, revision);

        // Assert
        var entry = _context.Entry(entity);
        var revisionProperty = entry.Property(x => x.Revision);
        revisionProperty.OriginalValue.Should().Be(revision);
    }

    [Fact]
    public void TrySetOriginalRevision_WithModifiedEntity_ShouldPreserveModifiedState()
    {
        // Arrange
        var entity = new TestVersionableEntity { Id = Guid.NewGuid(), Revision = Guid.NewGuid() };
        var revision = Guid.NewGuid();
        
        // Add entity to context and mark as modified
        _context.Entry(entity).State = EntityState.Added;
        _context.Entry(entity).Property(x => x.Revision).IsModified = true;

        // Act
        _context.TrySetOriginalRevision(entity, revision);

        // Assert
        var entry = _context.Entry(entity);
        var revisionProperty = entry.Property(x => x.Revision);
        revisionProperty.IsModified.Should().BeTrue();
    }

    [Fact]
    public void TrySetOriginalRevision_WithUnmodifiedEntity_ShouldPreserveUnmodifiedState()
    {
        // Arrange
        var entity = new TestVersionableEntity { Id = Guid.NewGuid(), Revision = Guid.NewGuid() };
        var revision = Guid.NewGuid();
        
        // Add entity to context and ensure it's not modified
        _context.Entry(entity).State = EntityState.Added;
        _context.Entry(entity).Property(x => x.Revision).IsModified = false;

        // Act
        _context.TrySetOriginalRevision(entity, revision);

        // Assert
        var entry = _context.Entry(entity);
        var revisionProperty = entry.Property(x => x.Revision);
        revisionProperty.IsModified.Should().BeFalse();
    }

    [Fact]
    public void MustWhenModified_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestEntity entity = null;

        // Act & Assert
        var action = () => _context.MustWhenModified(entity, x => x.Name, name => name.Length > 0);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MustWhenModified_WithNullPropertyExpression_ShouldThrowArgumentNullException()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act & Assert
        var action = () => _context.MustWhenModified(entity, (Expression<Func<TestEntity, string>>)null, name => name.Length > 0);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MustWhenModified_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;

        // Act & Assert
        var action = () => _context.MustWhenModified(entity, x => x.Name, null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MustWhenModified_WithUnmodifiedProperty_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;
        _context.Entry(entity).Property(x => x.Name).IsModified = false;

        // Act
        var result = _context.MustWhenModified(entity, x => x.Name, name => name.Length > 0);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MustWhenModified_WithModifiedPropertyAndTruePredicate_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;
        _context.Entry(entity).Property(x => x.Name).IsModified = true;

        // Act
        var result = _context.MustWhenModified(entity, x => x.Name, name => name.Length > 0);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MustWhenModified_WithModifiedPropertyAndFalsePredicate_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        _context.Entry(entity).State = EntityState.Added;
        _context.Entry(entity).Property(x => x.Name).IsModified = true;

        // Act
        var result = _context.MustWhenModified(entity, x => x.Name, name => name.Length > 10);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MustWhenModified_WithModifiedPropertyAndNullValue_ShouldEvaluatePredicate()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = null };
        _context.Entry(entity).State = EntityState.Added;
        _context.Entry(entity).Property(x => x.Name).IsModified = true;

        // Act
        var result = _context.MustWhenModified(entity, x => x.Name, name => name == null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetExistingIds_WithNullEntities_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<TestEntity> entities = null;

        // Act & Assert
        var action = async () => await _context.GetExistingIds(entities, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetExistingIds_WithEmptyCollection_ShouldReturnEmptyList()
    {
        // Arrange
        var entities = new List<TestEntity>();

        // Act
        var result = await _context.GetExistingIds(entities, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExistingIds_WithEntitiesWithDefaultIds_ShouldReturnEmptyList()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = default, Name = "Test1" },
            new() { Id = default, Name = "Test2" }
        };

        // Act
        var result = await _context.GetExistingIds(entities, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExistingIds_WithNonExistentIds_ShouldReturnEmptyList()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test1" },
            new() { Id = Guid.NewGuid(), Name = "Test2" }
        };

        // Act
        var result = await _context.GetExistingIds(entities, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExistingIds_WithExistingIds_ShouldReturnMatchingIds()
    {
        // Arrange
        var existingEntity = new TestEntity { Id = Guid.NewGuid(), Name = "Existing" };
        _context.Set<TestEntity>().Add(existingEntity);
        await _context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = existingEntity.Id, Name = "Test1" },
            new() { Id = Guid.NewGuid(), Name = "Test2" }
        };

        // Act
        var result = await _context.GetExistingIds(entities, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().Contain(existingEntity.Id);
    }

    [Fact]
    public async Task GetExistingIds_WithMixedExistingAndNonExistingIds_ShouldReturnOnlyExistingIds()
    {
        // Arrange
        var existingEntity1 = new TestEntity { Id = Guid.NewGuid(), Name = "Existing1" };
        var existingEntity2 = new TestEntity { Id = Guid.NewGuid(), Name = "Existing2" };
        _context.Set<TestEntity>().AddRange(existingEntity1, existingEntity2);
        await _context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = existingEntity1.Id, Name = "Test1" },
            new() { Id = Guid.NewGuid(), Name = "Test2" },
            new() { Id = existingEntity2.Id, Name = "Test3" },
            new() { Id = Guid.NewGuid(), Name = "Test4" }
        };

        // Act
        var result = await _context.GetExistingIds(entities, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(existingEntity1.Id);
        result.Should().Contain(existingEntity2.Id);
    }

    [Fact]
    public async Task GetExistingIds_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Test1" }
        };
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        var action = async () => await _context.GetExistingIds(entities, cancellationTokenSource.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetExistingIds_WithDuplicateIds_ShouldReturnDistinctIds()
    {
        // Arrange
        var existingEntity = new TestEntity { Id = Guid.NewGuid(), Name = "Existing" };
        _context.Set<TestEntity>().Add(existingEntity);
        await _context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = existingEntity.Id, Name = "Test1" },
            new() { Id = existingEntity.Id, Name = "Test2" }
        };

        // Act
        var result = await _context.GetExistingIds(entities, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().Contain(existingEntity.Id);
    }

    [Fact]
    public async Task GetExistingIds_WithIgnoreQueryFilters_ShouldIncludeDeletedEntities()
    {
        // Arrange
        var existingEntity = new TestEntity { Id = Guid.NewGuid(), Name = "Existing" };
        _context.Set<TestEntity>().Add(existingEntity);
        await _context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = existingEntity.Id, Name = "Test1" }
        };

        // Act
        var result = await _context.GetExistingIds(entities, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().Contain(existingEntity.Id);
    }

    [Fact]
    public void OnModelCreating_ShouldConfigureModelCorrectly()
    {
        // Arrange & Act
        var context = new DialogDbContext(_options);

        // Assert
        // Verify that the model is created successfully
        context.Model.Should().NotBeNull();
        
        // Verify that Actor entity is registered
        var actorEntityType = context.Model.FindEntityType(typeof(Actor));
        actorEntityType.Should().NotBeNull();
    }

    [Fact]
    public void Context_ShouldImplementIDialogDbContext()
    {
        // Assert
        _context.Should().BeAssignableTo<IDialogDbContext>();
    }

    [Fact]
    public void Context_ShouldInheritFromDbContext()
    {
        // Assert
        _context.Should().BeAssignableTo<DbContext>();
    }

    [Theory]
    [InlineData(typeof(DialogEntity))]
    [InlineData(typeof(DialogStatus))]
    [InlineData(typeof(DialogActivity))]
    [InlineData(typeof(DialogActivityType))]
    [InlineData(typeof(DialogTransmission))]
    [InlineData(typeof(DialogTransmissionType))]
    [InlineData(typeof(DialogTransmissionContent))]
    [InlineData(typeof(DialogTransmissionContentType))]
    [InlineData(typeof(DialogApiAction))]
    [InlineData(typeof(DialogApiActionEndpoint))]
    [InlineData(typeof(DialogGuiAction))]
    [InlineData(typeof(DialogGuiActionPriority))]
    [InlineData(typeof(DialogSeenLog))]
    [InlineData(typeof(DialogUserType))]
    [InlineData(typeof(DialogSearchTag))]
    [InlineData(typeof(DialogContent))]
    [InlineData(typeof(DialogContentType))]
    [InlineData(typeof(SubjectResource))]
    [InlineData(typeof(DialogEndUserContext))]
    [InlineData(typeof(DialogServiceOwnerContext))]
    [InlineData(typeof(DialogServiceOwnerLabel))]
    [InlineData(typeof(LabelAssignmentLog))]
    [InlineData(typeof(NotificationAcknowledgement))]
    [InlineData(typeof(ResourcePolicyInformation))]
    [InlineData(typeof(ActorName))]
    [InlineData(typeof(DialogAttachment))]
    public void EntityTypes_ShouldBeRegisteredInModel(Type entityType)
    {
        // Act
        var entityTypeInModel = _context.Model.FindEntityType(entityType);

        // Assert
        entityTypeInModel.Should().NotBeNull($"Entity type {entityType.Name} should be registered in the model");
    }

    [Fact]
    public void Database_ShouldBeAccessible()
    {
        // Assert
        _context.Database.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldBeCallable()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        _context.Set<TestEntity>().Add(entity);

        // Act
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ChangeTracker_ShouldBeAccessible()
    {
        // Assert
        _context.ChangeTracker.Should().NotBeNull();
    }

    [Fact]
    public void Entry_ShouldReturnValidEntityEntry()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var entry = _context.Entry(entity);

        // Assert
        entry.Should().NotBeNull();
        entry.Entity.Should().Be(entity);
    }

    [Fact]
    public void Set_ShouldReturnValidDbSet()
    {
        // Act
        var set = _context.Set<TestEntity>();

        // Assert
        set.Should().NotBeNull();
        set.Should().BeAssignableTo<DbSet<TestEntity>>();
    }

    // Test helper classes
    public class TestEntity : IIdentifiableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestVersionableEntity : IVersionableEntity, IIdentifiableEntity
    {
        public Guid Id { get; set; }
        public Guid Revision { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}

// Additional integration tests for database-specific functionality
public class DialogDbContextIntegrationTests : IDisposable
{
    private readonly DialogDbContext _context;
    private readonly DbContextOptions<DialogDbContext> _options;

    public DialogDbContextIntegrationTests()
    {
        _options = new DbContextOptionsBuilder<DialogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new DialogDbContext(_options);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task SaveChangesAsync_WithValidEntity_ShouldPersistToDatabase()
    {
        // Arrange
        var entity = new DialogDbContextTests.TestEntity 
        { 
            Id = Guid.NewGuid(), 
            Name = "Test Entity" 
        };
        _context.Set<DialogDbContextTests.TestEntity>().Add(entity);

        // Act
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        
        var savedEntity = await _context.Set<DialogDbContextTests.TestEntity>()
            .FindAsync(entity.Id);
        savedEntity.Should().NotBeNull();
        savedEntity.Name.Should().Be("Test Entity");
    }

    [Fact]
    public async Task Database_ShouldBeAccessible()
    {
        // Act & Assert
        _context.Database.Should().NotBeNull();
        var canConnect = await _context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }

    [Fact]
    public void Context_ShouldHandleMultipleInstances()
    {
        // Arrange & Act
        using var context1 = new DialogDbContext(_options);
        using var context2 = new DialogDbContext(_options);

        // Assert
        context1.Should().NotBeSameAs(context2);
        context1.Should().NotBeNull();
        context2.Should().NotBeNull();
    }

    [Fact]
    public async Task GetExistingIds_WithLargeDataSet_ShouldPerformEfficiently()
    {
        // Arrange
        var existingEntities = Enumerable.Range(1, 100)
            .Select(i => new DialogDbContextTests.TestEntity 
            { 
                Id = Guid.NewGuid(), 
                Name = $"Entity {i}" 
            })
            .ToList();

        _context.Set<DialogDbContextTests.TestEntity>().AddRange(existingEntities);
        await _context.SaveChangesAsync();

        var testEntities = existingEntities.Take(50)
            .Concat(Enumerable.Range(101, 50)
                .Select(i => new DialogDbContextTests.TestEntity 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"New Entity {i}" 
                }))
            .ToList();

        // Act
        var result = await _context.GetExistingIds(testEntities, CancellationToken.None);

        // Assert
        result.Should().HaveCount(50);
        result.Should().OnlyContain(id => existingEntities.Any(e => e.Id == id));
    }

    [Fact]
    public async Task Context_ShouldHandleTransactionScope()
    {
        // Arrange
        var entity = new DialogDbContextTests.TestEntity 
        { 
            Id = Guid.NewGuid(), 
            Name = "Transaction Test" 
        };

        // Act
        using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Set<DialogDbContextTests.TestEntity>().Add(entity);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Assert
        var savedEntity = await _context.Set<DialogDbContextTests.TestEntity>()
            .FindAsync(entity.Id);
        savedEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task Context_ShouldHandleTransactionRollback()
    {
        // Arrange
        var entity = new DialogDbContextTests.TestEntity 
        { 
            Id = Guid.NewGuid(), 
            Name = "Rollback Test" 
        };

        // Act
        using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Set<DialogDbContextTests.TestEntity>().Add(entity);
        await _context.SaveChangesAsync();
        await transaction.RollbackAsync();

        // Assert
        var savedEntity = await _context.Set<DialogDbContextTests.TestEntity>()
            .FindAsync(entity.Id);
        savedEntity.Should().BeNull();
    }

    [Fact]
    public async Task Context_ShouldHandleConcurrentOperations()
    {
        // Arrange
        var entities = Enumerable.Range(1, 10)
            .Select(i => new DialogDbContextTests.TestEntity 
            { 
                Id = Guid.NewGuid(), 
                Name = $"Concurrent Entity {i}" 
            })
            .ToList();

        // Act
        var tasks = entities.Select(async entity =>
        {
            using var context = new DialogDbContext(_options);
            context.Set<DialogDbContextTests.TestEntity>().Add(entity);
            await context.SaveChangesAsync();
        });

        await Task.WhenAll(tasks);

        // Assert
        var count = await _context.Set<DialogDbContextTests.TestEntity>().CountAsync();
        count.Should().Be(10);
    }

    [Fact]
    public async Task Context_ShouldHandleEntityStateChanges()
    {
        // Arrange
        var entity = new DialogDbContextTests.TestEntity 
        { 
            Id = Guid.NewGuid(), 
            Name = "State Test" 
        };

        // Act & Assert - Added state
        _context.Set<DialogDbContextTests.TestEntity>().Add(entity);
        _context.Entry(entity).State.Should().Be(EntityState.Added);

        await _context.SaveChangesAsync();
        _context.Entry(entity).State.Should().Be(EntityState.Unchanged);

        // Act & Assert - Modified state
        entity.Name = "Modified Name";
        _context.Entry(entity).State.Should().Be(EntityState.Modified);

        await _context.SaveChangesAsync();
        _context.Entry(entity).State.Should().Be(EntityState.Unchanged);

        // Act & Assert - Deleted state
        _context.Set<DialogDbContextTests.TestEntity>().Remove(entity);
        _context.Entry(entity).State.Should().Be(EntityState.Deleted);
    }
}

// Performance and stress tests
public class DialogDbContextPerformanceTests : IDisposable
{
    private readonly DialogDbContext _context;
    private readonly DbContextOptions<DialogDbContext> _options;

    public DialogDbContextPerformanceTests()
    {
        _options = new DbContextOptionsBuilder<DialogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new DialogDbContext(_options);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task GetExistingIds_WithManyEntities_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var existingEntities = Enumerable.Range(1, 1000)
            .Select(i => new DialogDbContextTests.TestEntity 
            { 
                Id = Guid.NewGuid(), 
                Name = $"Entity {i}" 
            })
            .ToList();

        _context.Set<DialogDbContextTests.TestEntity>().AddRange(existingEntities);
        await _context.SaveChangesAsync();

        var testEntities = existingEntities.Take(500)
            .Concat(Enumerable.Range(1001, 500)
                .Select(i => new DialogDbContextTests.TestEntity 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"New Entity {i}" 
                }))
            .ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _context.GetExistingIds(testEntities, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        result.Should().HaveCount(500);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task TrySetOriginalRevision_WithManyEntities_ShouldPerformWell()
    {
        // Arrange
        var entities = Enumerable.Range(1, 100)
            .Select(i => new DialogDbContextTests.TestVersionableEntity 
            { 
                Id = Guid.NewGuid(), 
                Revision = Guid.NewGuid()
            })
            .ToList();

        foreach (var entity in entities)
        {
            _context.Entry(entity).State = EntityState.Added;
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = entities.Select(entity => 
            _context.TrySetOriginalRevision(entity, Guid.NewGuid()))
            .ToList();
        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeTrue());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }
}