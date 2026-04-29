namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1;

public partial class AcceptedLanguages
{
    public override string ToString() =>
        AcceptedLanguage is null || AcceptedLanguage.Count == 0
            ? string.Empty
            : string.Join(", ", AcceptedLanguage.Select(l => l.ToString()));
}

public partial class AcceptedLanguage
{
    public override string ToString() =>
        Weight >= 100
            ? LanguageCode
            : $"{LanguageCode};q={Weight / 100.0:0.##}";
}
