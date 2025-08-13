using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Localization
{
    public const string CopyCommand = """COPY "Localization" ("LanguageCode", "LocalizationSetId", "CreatedAt", "UpdatedAt", "Value") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var csvData = new StringBuilder();

        List<Guid> localizationSetIds =
        [
            dto.DialogId,
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 1),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), 2),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 1),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 2),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction), 3),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogActivity), Activity.DialogCreatedType),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(DialogActivity), Activity.InformationType),
            DeterministicUuidV7.Generate(dto.Timestamp,
                nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent), 1),
            DeterministicUuidV7.Generate(dto.Timestamp,
                nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent), 2),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents.DialogTransmissionContent), 1),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents.DialogTransmissionContent), 2),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents.DialogTransmissionContent), 3),
            DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents.DialogTransmissionContent), 4)
        ];

        foreach (var localizationSetId in localizationSetIds)
        {
            var rng = new Random(localizationSetId.GetHashCode());

            var english = Words.English.Length != 0
                ? Words.English[rng.Next(0, Words.English.Length)] + " " +
                  Words.English[rng.Next(0, Words.English.Length)]
                : $"English {Guid.NewGuid().ToString()[..8]}";

            var norwegian = Words.Norwegian.Length != 0
                ? Words.Norwegian[rng.Next(0, Words.Norwegian.Length)] + " " +
                  Words.Norwegian[rng.Next(0, Words.Norwegian.Length)]
                : $"Norsk {Guid.NewGuid().ToString()[..8]}";

            csvData.AppendLine(
                $"nb,{localizationSetId},{dto.FormattedTimestamp},{dto.FormattedTimestamp},{norwegian}");
            csvData.AppendLine(
                $"en,{localizationSetId},{dto.FormattedTimestamp},{dto.FormattedTimestamp},{english}");
        }

        return csvData.ToString();
    }
}
