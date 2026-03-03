// ReSharper disable InconsistentNaming
namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1;

public partial class V1EndUserCommon_AcceptedLanguages
{
    public override string ToString() =>
        AcceptedLanguage.Count == 0
            ? string.Empty
            : string.Join(", ", AcceptedLanguage.Select(l => l.ToString()));
}

public partial class V1EndUserCommon_AcceptedLanguage
{
    // Weight is 0–100 scale matching the server's internal representation (q * 100)
    public override string ToString() =>
        Weight >= 100
            ? LanguageCode
            : $"{LanguageCode};q={Weight / 100.0:0.##}";
}
