using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.Activities;

internal sealed class DialogActivityConfiguration : IEntityTypeConfiguration<DialogActivity>
{
    public void Configure(EntityTypeBuilder<DialogActivity> builder)
    {
        builder.HasIndex(x => new { x.DialogId, x.CreatedAt, x.Id })
            // EF will by convention create an index named IX_DialogActivity_DialogId, as it is a foreign key.
            // This annotation ensures that the convention-created index is not created in addition to this one.
            .ReplacesIndex("IX_DialogActivity_DialogId")
            .HasDatabaseName("IX_DialogActivity_DialogId_CreatedAt_Id")
            .IsDescending(false, true, true);

        builder.HasIndex(x => new { x.TransmissionId, x.TypeId })
            // EF will by convention create an index named IX_DialogActivity_TransmissionId, as it is a foreign key.
            // This annotation ensures that the convention-created index is not created in addition to this one.
            .ReplacesIndex("IX_DialogActivity_TransmissionId")
            .HasDatabaseName("IX_DialogActivity_TransmissionId_TypeId")
            .IsDescending(false, false);

        builder.HasOne(x => x.Transmission)
            .WithMany(x => x.Activities)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
