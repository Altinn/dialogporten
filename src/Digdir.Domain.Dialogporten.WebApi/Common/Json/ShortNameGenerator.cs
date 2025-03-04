using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using NJsonSchema.Generation;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

internal sealed class ShortNameGenerator : ISchemaNameGenerator
{
    public string Generate(Type type) => TypeNameConverter.ToShortName(type);
}
