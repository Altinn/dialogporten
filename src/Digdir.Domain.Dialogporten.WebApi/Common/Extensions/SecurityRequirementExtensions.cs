using NSwag;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

public static class SecurityRequirementExtensions
{
    public enum ScopeRequirementOperation
    {
        And,
        Or
    }

    extension(ICollection<OpenApiSecurityRequirement> requirements)
    {
        public void Add((ScopeRequirementOperation Operation, string[] Scopes) requirement, string name)
        {
            switch (requirement.Operation)
            {
                case ScopeRequirementOperation.And:
                    requirements.Add(new OpenApiSecurityRequirement
                    {
                        [name] = requirement.Scopes
                    });
                    break;
                case ScopeRequirementOperation.Or:
                    foreach (var scope in requirement.Scopes)
                    {
                        requirements.Add(new OpenApiSecurityRequirement
                        {
                            [name] = [scope]
                        });
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
