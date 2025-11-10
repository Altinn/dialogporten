using System.Data;
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
using System.Text;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Development;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.IdempotentNotifications;
using EntityFramework.Exceptions.PostgreSQL;
using MassTransit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence;

internal sealed class DialogDbContext : DbContext, IDialogDbContext
{
    public DialogDbContext(DbContextOptions<DialogDbContext> options) : base(options) { }

    public DbSet<DialogEntity> Dialogs => Set<DialogEntity>();
    public DbSet<DialogStatus> DialogStatuses => Set<DialogStatus>();
    public DbSet<DialogActivity> DialogActivities => Set<DialogActivity>();
    public DbSet<DialogActivityType> DialogActivityTypes => Set<DialogActivityType>();
    public DbSet<DialogTransmission> DialogTransmissions => Set<DialogTransmission>();
    public DbSet<DialogTransmissionType> DialogTransmissionTypes => Set<DialogTransmissionType>();
    public DbSet<DialogTransmissionContent> DialogTransmissionContents => Set<DialogTransmissionContent>();
    public DbSet<DialogTransmissionContentType> DialogTransmissionContentTypes => Set<DialogTransmissionContentType>();
    public DbSet<DialogApiAction> DialogApiActions => Set<DialogApiAction>();
    public DbSet<DialogApiActionEndpoint> DialogApiActionEndpoints => Set<DialogApiActionEndpoint>();
    public DbSet<DialogGuiAction> DialogGuiActions => Set<DialogGuiAction>();
    public DbSet<DialogGuiActionPriority> DialogGuiActionPriority => Set<DialogGuiActionPriority>();
    public DbSet<DialogSeenLog> DialogSeenLog => Set<DialogSeenLog>();
    public DbSet<DialogUserType> DialogUserTypes => Set<DialogUserType>();
    public DbSet<DialogSearchTag> DialogSearchTags => Set<DialogSearchTag>();
    public DbSet<DialogContent> DialogContents => Set<DialogContent>();
    public DbSet<DialogContentType> DialogContentTypes => Set<DialogContentType>();
    public DbSet<SubjectResource> SubjectResources => Set<SubjectResource>();
    public DbSet<DialogEndUserContext> DialogEndUserContexts => Set<DialogEndUserContext>();
    public DbSet<SystemLabel> SystemLabels => Set<SystemLabel>();

    public DbSet<DialogServiceOwnerContext> DialogServiceOwnerContexts => Set<DialogServiceOwnerContext>();
    public DbSet<DialogServiceOwnerLabel> DialogServiceOwnerLabels => Set<DialogServiceOwnerLabel>();
    public DbSet<LabelAssignmentLog> LabelAssignmentLogs => Set<LabelAssignmentLog>();
    public DbSet<NotificationAcknowledgement> NotificationAcknowledgements => Set<NotificationAcknowledgement>();
    public DbSet<ResourcePolicyInformation> ResourcePolicyInformation => Set<ResourcePolicyInformation>();
    public DbSet<ActorName> ActorName => Set<ActorName>();
    public DbSet<DialogAttachment> DialogAttachments => Set<DialogAttachment>();


    private static readonly StringBuilder QueryLog = new();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        optionsBuilder.AddInterceptors(new DevelopmentCommandLineQueryWriter(x => QueryLog.AppendLine(x)));
        optionsBuilder.UseExceptionProcessor();
    }

    internal bool TrySetOriginalRevision<TEntity>(
        TEntity? entity,
        Guid? revision)
        where TEntity : class, IVersionableEntity
    {
        if (entity is null || !revision.HasValue)
        {
            return false;
        }

        var prop = Entry(entity).Property(x => x.Revision);
        var isModified = prop.IsModified;
        prop.OriginalValue = revision.Value;
        prop.IsModified = isModified;
        return true;
    }

    /// <inheritdoc/>
    public bool MustWhenModified<TEntity, TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Func<TProperty, bool> predicate)
        where TEntity : class
    {
        var property = Entry(entity).Property(propertyExpression);
        return !property.IsModified || predicate(property.CurrentValue);
    }

    public async Task<List<Guid>> GetExistingIds<TEntity>(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken)
        where TEntity : class, IIdentifiableEntity
    {
        var ids = entities
            .Select(x => x.Id)
            .Where(x => x != default)
            .ToList();

        return ids.Count == 0
            ? []
            : await Set<TEntity>()
                .IgnoreQueryFilters()
                .Select(x => x.Id)
                .Where(x => ids.Contains(x))
                .ToListAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>(x => x.HaveMaxLength(Domain.Common.Constants.DefaultMaxStringLength));
        configurationBuilder.Properties<Uri>(x => x.HaveMaxLength(Domain.Common.Constants.DefaultMaxUriLength));
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetConverter>();
        configurationBuilder.Properties<TimeSpan>().HaveConversion<TimeSpanToStringConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicitly configure the Actor entity so that it will register as TPH in the database
        modelBuilder.Entity<Actor>();

        modelBuilder
            .HasPostgresExtension(Constants.PostgreSqlTrigram)
            .RemovePluralizingTableNameConvention()
            .AddAuditableEntities()
            .ApplyConfigurationsFromAssembly(typeof(DialogDbContext).Assembly)
            .AddTransactionalOutboxEntities(builder =>
            {
                builder.ToTable($"MassTransit{builder.Metadata.GetTableName()}");
            });
    }

    public Task<T> WrapWithRepeatableRead<T>(
        Func<IDialogDbContext, CancellationToken, Task<T>> queryFunc,
        CancellationToken cancellationToken) =>
        WrapWithIsolationLevel(IsolationLevel.RepeatableRead, queryFunc, cancellationToken);

    private async Task<T> WrapWithIsolationLevel<T>(
        IsolationLevel level,
        Func<IDialogDbContext, CancellationToken, Task<T>> queryFunc,
        CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("Cannot start a new transaction when there is already an active transaction.");
        }

        await using var transaction = await Database.BeginTransactionAsync(level, cancellationToken);
        var result = await queryFunc(this, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

}
