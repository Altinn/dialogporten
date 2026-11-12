namespace Digdir.Library.Utils.AspNet;

/// <summary>
/// Endpoint metadata that lets requests through to the endpoint while maintenance mode is on. Meant
/// for endpoints that the platform or deploy tooling calls rather than API clients, such as the
/// health probes. Apply it with <c>AllowDuringMaintenance()</c> on the endpoint's convention builder.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class AllowDuringMaintenanceAttribute : Attribute;
