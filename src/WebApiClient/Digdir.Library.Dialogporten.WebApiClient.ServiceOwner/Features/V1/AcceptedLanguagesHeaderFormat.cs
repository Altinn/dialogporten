using Altinn.ApiClients.Dialogporten.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1;

public partial class AcceptedLanguages
{
    public override string ToString() =>
        AcceptedLanguagesHeaderFormatter.FormatAcceptedLanguages(
            AcceptedLanguage,
            static language => language.ToString());
}

public partial class AcceptedLanguage
{
    public override string ToString() =>
        AcceptedLanguagesHeaderFormatter.FormatAcceptedLanguage(LanguageCode, Weight);
}
