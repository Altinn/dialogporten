using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs;

internal sealed class DialogRelationConfiguration : IEntityTypeConfiguration<DialogRelation>
{
    public void Configure(EntityTypeBuilder<DialogRelation> builder)
    {
        // One canonical row per relation: the lower id is always stored in DialogId. Together with the primary key
        // this enforces one relation per pair (whatever the type) and rules out self-relations.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DialogRelation_DialogId_LessThan_RelatedDialogId",
            $"\"{nameof(DialogRelation.DialogId)}\" < \"{nameof(DialogRelation.RelatedDialogId)}\""));

        builder.HasKey(x => new { x.DialogId, x.RelatedDialogId });

        // Reads the relation from its other side, and backs the cascade on the second foreign key.
        builder.HasIndex(x => new { x.RelatedDialogId, x.DialogId });

        builder.HasOne(x => x.Dialog)
            .WithMany()
            .HasForeignKey(x => x.DialogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RelatedDialog)
            .WithMany()
            .HasForeignKey(x => x.RelatedDialogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
