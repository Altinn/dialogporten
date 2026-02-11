namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal interface IDialogEndUserSearchStrategy
{
    string Name { get; }
    int Score(EndUserSearchContext context);
    PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context);
}
