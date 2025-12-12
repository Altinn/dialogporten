namespace Digdir.Library.Utils.AspNet;

public sealed class AspNetSettings
{
    public const string ConfigurationSectionName = "AspNetSettings";

    public FeatureToggle FeatureToggle { get; init; } = new();
}

public sealed class FeatureToggle
{
    public bool PresentationLayerMaintenanceMode { get; set; }
}
