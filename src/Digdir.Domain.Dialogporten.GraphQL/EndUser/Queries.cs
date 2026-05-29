using Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;
using HotChocolate.Authorization;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

[Authorize(Policy = AuthorizationPolicy.EndUser)]
public sealed partial class Queries;
