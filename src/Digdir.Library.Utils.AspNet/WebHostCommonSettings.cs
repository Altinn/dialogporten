namespace Digdir.Library.Utils.AspNet;

public sealed class WebHostCommonSettings
{
    public const string SectionName = "WebHostCommon";

    public MaintenanceMode MaintenanceMode { get; init; } = new();
}

public sealed class MaintenanceMode
{
    public bool Enabled { get; set; }
    public DateTimeOffset? RetryAt { get; set; }
}
