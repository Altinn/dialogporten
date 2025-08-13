using System.Text;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogTransmission
{
    public static readonly string CopyCommand = Create(nameof(DialogTransmission),
        "Id", "CreatedAt", "AuthorizationAttribute", "ExtendedType", "TypeId", "DialogId", "RelatedTransmissionId");

    public static List<TransmissionDto> GetDtos(DialogTimestamp dto)
    {
        var numTransmissions = dto.GetRng().Next(0, 2);

        if (numTransmissions == 0)
        {
            return [];
        }

        var transmissionDtos = new List<TransmissionDto>(numTransmissions);
        for (var i = 1; i <= numTransmissions; i++)
        {
            var transmissionId = DeterministicUuidV7.Generate(dto.Timestamp, nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission), i);
            transmissionDtos.Add(new TransmissionDto(transmissionId, i));
        }

        return transmissionDtos;
    }

    public record TransmissionDto(Guid Id, int TypeId);


    public static string Generate(DialogTimestamp dto)
    {
        var transmissionCsvData = new StringBuilder();

        var transmissionDtos = GetDtos(dto);

        foreach (var (transmission, index) in transmissionDtos.Select((t, i) => (t, i)))
        {
            var relatedTransmissionId = index == 0 ? string.Empty : transmissionDtos[index - 1].Id.ToString();
            transmissionCsvData.AppendLine($"{transmission.Id},{dto.FormattedTimestamp},,,{transmission.TypeId},{dto.DialogId},{relatedTransmissionId}");
        }

        return transmissionCsvData.ToString();
    }
}
