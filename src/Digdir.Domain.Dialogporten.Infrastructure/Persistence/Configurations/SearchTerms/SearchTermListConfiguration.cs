using Digdir.Domain.Dialogporten.Domain.SearchTerms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.SearchTerms;

internal sealed class SearchTermListConfiguration : IEntityTypeConfiguration<SearchTermList>
{
    public void Configure(EntityTypeBuilder<SearchTermList> builder)
    {
        var words = builder.Property(x => x.Words).HasColumnType("jsonb");
        // The jsonb payload is unbounded; clear the global 255-char string convention (HaveMaxLength)
        // so the model doesn't falsely advertise a length limit on a JSON document column.
        words.Metadata.SetMaxLength(null);

        builder.HasIndex(x => x.Language).IsUnique();
    }
}
