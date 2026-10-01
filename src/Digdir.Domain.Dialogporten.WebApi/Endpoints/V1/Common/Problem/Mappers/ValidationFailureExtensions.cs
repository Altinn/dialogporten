using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflicts;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Mappers;

public static class ValidationFailuresExtensions
{
    extension(List<ValidationFailure> failures)
    {
        public List<ProblemDetailsConflict> ToConflicts()
        {
            return failures
                .Select(x => ProblemDetailsConflictMapper.Map((IConflictReason)x.CustomState!))
                .ToList();
        }
    }
}
