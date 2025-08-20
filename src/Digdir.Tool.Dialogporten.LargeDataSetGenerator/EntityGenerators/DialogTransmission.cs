using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogTransmission
{
    public static readonly string CopyCommand = Create(nameof(DialogTransmission),
        "Id", "CreatedAt", "AuthorizationAttribute", "ExtendedType", "TypeId", "DialogId", "RelatedTransmissionId");

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission);
    public static List<TransmissionDto> GetDtos(DialogTimestamp dto)
    {
        var numTransmissions = dto.GetRng().Next(0, 5);

        if (numTransmissions == 0)
        {
            return [];
        }

        List<DialogTransmissionType.Values> types = [
            DialogTransmissionType.Values.Information,
            DialogTransmissionType.Values.Submission,
            DialogTransmissionType.Values.Rejection,
            DialogTransmissionType.Values.Correction];

        var transmissionDtos = new List<TransmissionDto>(numTransmissions);
        for (var i = 0; i < numTransmissions; i++)
        {
            var transmissionId = DeterministicUuidV7.Generate(dto.Timestamp, DomainName, i);
            var typeId = types[i % types.Count];
            transmissionDtos.Add(new TransmissionDto(transmissionId, typeId));
        }

        return transmissionDtos;
    }

    public sealed record TransmissionDto(Guid Id, DialogTransmissionType.Values TypeId);

    public static string Generate(DialogTimestamp dto)
    {
        var transmissionCsvData = new StringBuilder();

        var transmissionDtos = GetDtos(dto);

        foreach (var (transmission, index) in transmissionDtos.Select((t, i) => (t, i)))
        {
            var relatedTransmissionId = index == 0 ? string.Empty : transmissionDtos[index - 1].Id.ToString();
            transmissionCsvData.AppendLine($"{transmission.Id},{dto.FormattedTimestamp},,,{(int)transmission.TypeId},{dto.DialogId},{relatedTransmissionId}");
        }

        return transmissionCsvData.ToString();
    }
}
