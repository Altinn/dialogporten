using Altinn.ApiClients.Dialogporten.Common;

namespace Altinn.ApiClients.Dialogporten.Features.V1;

#pragma warning disable CA1707
public partial class V1EndUserCommon_AcceptedLanguages
{
    public override string ToString() =>
        AcceptedLanguagesHeaderFormatter.FormatAcceptedLanguages(
            AcceptedLanguage,
            static language => language.ToString());
}

public partial class V1EndUserCommon_AcceptedLanguage
{
    public override string ToString() => AcceptedLanguagesHeaderFormatter.FormatAcceptedLanguage(LanguageCode, Weight);
}
#pragma warning restore CA1707
