namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

internal sealed record Settings(
    string ConnectionString,
    int MaxPostgresConnections,
    int DialogAmount,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string AltinnPlatformBaseUrl)
{
    public void Validate()
    {
        if (DialogAmount <= 0) throw new ArgumentOutOfRangeException(nameof(DialogAmount), "Must be greater than zero.");
        if (MaxPostgresConnections <= 0) throw new ArgumentOutOfRangeException(nameof(MaxPostgresConnections), "Must be greater than zero.");
        if (FromDate >= ToDate) throw new ArgumentOutOfRangeException(nameof(FromDate), $"{nameof(FromDate)} must be earlier than {nameof(ToDate)}.");
        if (string.IsNullOrWhiteSpace(ConnectionString)) throw new ArgumentException("No connection string found", nameof(ConnectionString));
    }
}
