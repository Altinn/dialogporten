using System.ComponentModel.DataAnnotations;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Configuration options for cost management metrics system
/// </summary>
public sealed class CostManagementOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "CostManagement";

    /// <summary>
    /// Maximum number of transactions that can be queued for background processing.
    /// Recommended values:
    /// - Development: 1,000
    /// - Test: 10,000  
    /// - Production: 100,000-500,000 (depending on traffic)
    /// </summary>
    [Range(100, 1_000_000)]
    public int QueueCapacity { get; set; } = 100_000;


    /// <summary>
    /// Whether cost tracking is enabled (allows disabling per environment)
    /// </summary>
    public bool Enabled { get; set; } = true;
}
