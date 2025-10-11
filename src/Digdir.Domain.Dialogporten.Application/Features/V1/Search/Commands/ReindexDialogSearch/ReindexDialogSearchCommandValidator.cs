using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Search.Commands.ReindexDialogSearch;

internal sealed class ReindexDialogSearchCommandValidator : AbstractValidator<ReindexDialogSearchCommand>
{
    public ReindexDialogSearchCommandValidator()
    {
        RuleFor(x => x)
            .Must(x =>
            {
                var flags = 0;
                if (x.Full) flags++;
                if (x.Since.HasValue) flags++;
                if (x.Resume) flags++;
                if (x.StaleOnly) flags++;
                return flags == 1;
            })
            .WithMessage("Specify exactly one of: --full OR --since <ts> OR --resume OR --stale-only.");


        RuleFor(x => x.BatchSize).GreaterThan(0).When(x => x.BatchSize.HasValue);
        RuleFor(x => x.Workers).GreaterThan(0).When(x => x.Workers.HasValue);
        RuleFor(x => x.ThrottleMs).GreaterThanOrEqualTo(0).When(x => x.ThrottleMs.HasValue);
        RuleFor(x => x.WorkMemBytes).GreaterThan(0).When(x => x.WorkMemBytes.HasValue);
    }
}
