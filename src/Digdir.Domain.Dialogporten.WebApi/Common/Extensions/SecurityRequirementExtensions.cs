using NSwag;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

public static class SecurityRequirementExtensions
{

    extension(ICollection<OpenApiSecurityRequirement> requirements)
    {
        /// <summary>
        /// Adds the scopes to the open API scope requirement operation.
        /// This method doesn't support OR-ing scopes, this means that all scopes are required to perform the operation.
        /// </summary>
        public void Add(string[] scopes, string name)
        {
            requirements.Add(new OpenApiSecurityRequirement
            {
                [name] = scopes
            });
        }
    }
}
