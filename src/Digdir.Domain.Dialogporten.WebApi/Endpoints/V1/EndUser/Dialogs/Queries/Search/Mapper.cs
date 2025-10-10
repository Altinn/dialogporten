using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.Search;

internal static class Mapper
{
    public static SearchDialogQuery ToSearchDialogQuery(this SearchDialogRequest req) => new()
    {
        Org = req.Org,
        ServiceResource = req.ServiceResource,
        Party = req.Party,
        ExtendedStatus = req.ExtendedStatus,
        ExternalReference = req.ExternalReference,
        Status = req.Status,
        CreatedAfter = req.CreatedAfter,
        CreatedBefore = req.CreatedBefore,
        UpdatedAfter = req.UpdatedAfter,
        UpdatedBefore = req.UpdatedBefore,
        ContentUpdatedAfter = req.ContentUpdatedAfter,
        ContentUpdatedBefore = req.ContentUpdatedBefore,
        DueAfter = req.DueAfter,
        DueBefore = req.DueBefore,
        Process = req.Process,
        SystemLabel = req.SystemLabel,
        ExcludeApiOnly = req.ExcludeApiOnly,
        Search = req.Search,
        SearchLanguageCode = req.SearchLanguageCode,
        OrderBy = req.OrderBy,
        Limit = req.Limit,
        ContinuationToken = req.ContinuationToken,
        AcceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage
    };
}
