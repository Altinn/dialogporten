using System.Text.RegularExpressions;

namespace Digdir.Library.Utils.AspNet;

/// <summary>
/// Maps an outgoing HTTP request path to a route template for telemetry.
/// When <paramref name="PathPattern"/> matches an outgoing request's absolute path,
/// the dependency activity's display name is rewritten to use <paramref name="Template"/>
/// (with a leading slash) so high-cardinality URLs collapse to a single aggregate name.
/// </summary>
public sealed record HttpDependencyUrlTemplate(Regex PathPattern, string Template);
