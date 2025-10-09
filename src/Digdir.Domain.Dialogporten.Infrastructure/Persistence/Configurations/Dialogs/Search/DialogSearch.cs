using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions.Features.Updatable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.Search;

internal sealed class DialogSearch : IUpdateableEntity
{
    public Guid DialogId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DialogEntity Dialog { get; set; } = null!;
    public required NpgsqlTsVector SearchVector { get; set; }
}

internal sealed class DialogSearchConfiguration : IEntityTypeConfiguration<DialogSearch>
{
    public void Configure(EntityTypeBuilder<DialogSearch> builder)
    {
        builder.ToTable(nameof(DialogSearch), "search");
        builder.HasKey(ds => ds.DialogId);
        builder.HasOne(ds => ds.Dialog).WithOne()
            .HasForeignKey<DialogSearch>(ds => ds.DialogId)
            .HasPrincipalKey<DialogEntity>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(ds => ds.SearchVector).HasMethod("GIN");
    }
}
