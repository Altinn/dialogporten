using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

internal static class DialogTransmission
{
    public static readonly string CopyCommand = CreateCopyCommand(nameof(DialogTransmission),
        "Id", "CreatedAt", "AuthorizationAttribute", "ExtendedType", "TypeId", "DialogId", "RelatedTransmissionId");

    private static readonly List<DialogTransmissionType.Values> Types =
    [
        DialogTransmissionType.Values.Information,
        DialogTransmissionType.Values.Submission,
        DialogTransmissionType.Values.Rejection,
        DialogTransmissionType.Values.Correction,
        DialogTransmissionType.Values.Acceptance
    ];

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission);

    public static List<TransmissionDto> GetDtos(DialogTimestamp dto) => BuildDtoList<TransmissionDto>(dtos =>
    {
        var numTransmissions = dto.GetRng().Next(0, Types.Count + 1);

        if (numTransmissions == 0)
        {
            return;
        }

        for (var i = 0; i < numTransmissions; i++)
        {
            var transmissionId = dto.ToUuidV7(DomainName, i);
            var typeId = Types[i % Types.Count];
            dtos.Add(new TransmissionDto(transmissionId, typeId));
        }
    });

    public sealed record TransmissionDto(Guid Id, DialogTransmissionType.Values TypeId);

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        var transmissionDtos = GetDtos(dto);

        foreach (var (transmission, index) in transmissionDtos.Select((t, i) => (t, i)))
        {
            var relatedTransmissionId = index == 0 ? string.Empty : transmissionDtos[index - 1].Id.ToString();
            sb.AppendLine($"{transmission.Id},{dto.FormattedTimestamp},,,{(int)transmission.TypeId},{dto.DialogId},{relatedTransmissionId}");
        }
    });
}
