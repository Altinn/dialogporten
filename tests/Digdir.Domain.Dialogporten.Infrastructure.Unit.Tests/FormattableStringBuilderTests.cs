using System.Globalization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

// Reproduces https://github.com/Altinn/dialogporten/issues/4434
//
// FormattableStringBuilder.ConsolidateFormat renumbers an embedded fragment's placeholders with a
// prefix-matching string replace (Replace("{" + n, ...)). When a fragment has 11+ parameters the
// replace for "{1" also rewrites "{10", "{11", ... corrupting the placeholder. Formatting the
// resulting FormattableString (what EF does via FromSql) then either throws FormatException or binds
// the wrong argument. These tests fail before the fix and pass after.
public sealed class FormattableStringBuilderTests
{
    [Fact]
    public void Embedding_Fragment_With_Twelve_Parameters_Preserves_Mapping()
    {
        string o0 = "o0", o1 = "o1";
        string f0 = "f0", f1 = "f1", f2 = "f2", f3 = "f3", f4 = "f4", f5 = "f5",
            f6 = "f6", f7 = "f7", f8 = "f8", f9 = "f9", f10 = "f10", f11 = "f11";

        // Fragment with 12 distinct args -> placeholders {0}..{11}.
        var fragment = new PostgresFormattableStringBuilder()
            .Append($"{f0} {f1} {f2} {f3} {f4} {f5} {f6} {f7} {f8} {f9} {f10} {f11}");

        // Outer already holds two args, so the fragment is renumbered to offset 2. The renumber of "{1"
        // also corrupts "{10"/"{11", producing out-of-range placeholder indices.
        var outer = new PostgresFormattableStringBuilder()
            .Append($"{o0} {o1} {fragment}");

        var result = outer.ToFormattableString().ToString(CultureInfo.InvariantCulture);

        Assert.Equal("o0 o1 f0 f1 f2 f3 f4 f5 f6 f7 f8 f9 f10 f11", result);
    }
}
