using System.Runtime.CompilerServices;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.Mapping;

internal static class VerifyModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // The ObjectFiller produces deterministic Guids/dates, so keep the real values in the
        // snapshots instead of Verify's positional scrubbers - the concrete values make an
        // accidental field cross-wiring in a mapper obvious in the diff.
        VerifierSettings.DontScrubGuids();
        VerifierSettings.DontScrubDateTimes();
    }
}
