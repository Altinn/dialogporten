using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogDbContext
{
    DbSet<DialogEntity> Dialogs { get; }
    DbSet<DialogStatus> DialogStatuses { get; }

    DbSet<DialogActivity> DialogActivities { get; }
    DbSet<DialogActivityType> DialogActivityTypes { get; }

    DbSet<DialogTransmission> DialogTransmissions { get; }
    DbSet<DialogTransmissionType> DialogTransmissionTypes { get; }
    DbSet<DialogTransmissionContent> DialogTransmissionContents { get; }
    DbSet<DialogTransmissionContentType> DialogTransmissionContentTypes { get; }

    DbSet<DialogApiAction> DialogApiActions { get; }
    DbSet<DialogApiActionEndpoint> DialogApiActionEndpoints { get; }

    DbSet<DialogGuiAction> DialogGuiActions { get; }
    DbSet<DialogGuiActionPriority> DialogGuiActionPriority { get; }

    DbSet<DialogSeenLog> DialogSeenLog { get; }
    DbSet<DialogUserType> DialogUserTypes { get; }

    DbSet<DialogSearchTag> DialogSearchTags { get; }

    DbSet<DialogContent> DialogContents { get; }
    DbSet<DialogContentType> DialogContentTypes { get; }

    DbSet<SubjectResource> SubjectResources { get; }
    DbSet<DialogEndUserContext> DialogEndUserContexts { get; }
    DbSet<DialogServiceOwnerContext> DialogServiceOwnerContexts { get; }
    DbSet<DialogServiceOwnerLabel> DialogServiceOwnerLabels { get; }
    DbSet<LabelAssignmentLog> LabelAssignmentLogs { get; }
    DbSet<ResourcePolicyInformation> ResourcePolicyInformation { get; }

    DbSet<ActorName> ActorName { get; }
    DbSet<DialogAttachment> DialogAttachments { get; }

    /// <summary>
    /// Validate a property on the <typeparamref name="TEntity"/> using a lambda
    /// expression to specify the predicate only when the property is modified.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <param name="entity"></param>
    /// <param name="propertyExpression"></param>
    /// <param name="predicate"></param>
    /// <returns>
    ///     <para>False if the property is modified and the predicate returns false.</para>
    ///     <para>True if the property is unmodified or the predicate returns true.</para>
    /// </returns>
    bool MustWhenModified<TEntity, TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Func<TProperty, bool> predicate)
        where TEntity : class;
    Task<List<Guid>> GetExistingIds<TEntity>(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken)
        where TEntity : class, IIdentifiableEntity;

    DatabaseFacade GetDatabase();
}
