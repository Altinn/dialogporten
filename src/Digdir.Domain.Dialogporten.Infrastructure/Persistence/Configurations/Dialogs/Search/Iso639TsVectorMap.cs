using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.Search;

internal sealed class Iso639TsVectorMap
{
    private Iso639TsVectorMap() { }

    public Iso639TsVectorMap(string isoCode, string tsConfigName)
    {
        (IsoCode, TsConfigName) = (isoCode, tsConfigName);
    }

    public string IsoCode { get; private set; } = null!;
    public string TsConfigName { get; private set; } = null!;
}

internal sealed class Iso639TsVectorMapConfiguration : IEntityTypeConfiguration<Iso639TsVectorMap>
{
    public void Configure(EntityTypeBuilder<Iso639TsVectorMap> builder)
    {
        builder.ToTable(nameof(Iso639TsVectorMap), "search");
        builder.HasKey(m => m.IsoCode);
        builder.HasData(
            new("ar", "arabic"),
            new("hy", "armenian"),
            new("eu", "basque"),
            new("ca", "catalan"),
            new("da", "danish"),
            new("nl", "dutch"),
            new("en", "english"),
            new("fi", "finnish"),
            new("fr", "french"),
            new("de", "german"),
            new("el", "greek"),
            new("hi", "hindi"),
            new("hu", "hungarian"),
            new("id", "indonesian"),
            new("ga", "irish"),
            new("it", "italian"),
            new("lt", "lithuanian"),
            new("ne", "nepali"),
            new("nb", "norwegian"),
            new("nn", "norwegian"),
            new("no", "norwegian"),
            new("pt", "portuguese"),
            new("ro", "romanian"),
            new("ru", "russian"),
            new("sr", "serbian"),
            new("es", "spanish"),
            new("sv", "swedish"),
            new("ta", "tamil"),
            new("tr", "turkish"),
            new("yi", "yiddish"));
    }
}
