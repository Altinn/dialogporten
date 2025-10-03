using System.Linq.Expressions;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Common;

internal static class Expressions
{
    internal static Expression<Func<Localization, bool>> LocalizedSearchExpression(string? search, string? languageCode)
    {
        return localization =>
            (languageCode == null || localization.LanguageCode == languageCode) &&
            EF.Functions.ILike(localization.Value, $"%{search}%");
    }
}
