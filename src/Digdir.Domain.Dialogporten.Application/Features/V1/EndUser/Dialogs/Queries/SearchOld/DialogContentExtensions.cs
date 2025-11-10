using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchOld;

internal static class DialogContentExtensions
{
    public static void SetNonSensitiveContent(this List<DialogContent> content)
    {
        var nonSensitiveTitle = content.FirstOrDefault(x => x.TypeId == DialogContentType.Values.NonSensitiveTitle);
        if (nonSensitiveTitle is not null)
        {
            var title = content.First(x => x.TypeId == DialogContentType.Values.Title);
            title.Value = nonSensitiveTitle.Value;
        }

        var nonSensitiveSummary = content.FirstOrDefault(x => x.TypeId == DialogContentType.Values.NonSensitiveSummary);
        if (nonSensitiveSummary is not null)
        {
            var summary = content.First(x => x.TypeId == DialogContentType.Values.Summary);
            summary.Value = nonSensitiveSummary.Value;
        }
    }
}
