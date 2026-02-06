namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal interface IDialogEndUserSearchStrategy
{
    string Name { get; }
    EndUserSearchContext Context { get; }
    void SetContext(EndUserSearchContext context);
    int Score(EndUserSearchContext context);
    PostgresFormattableStringBuilder BuildSql();
}
