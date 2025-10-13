using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Search.Commands.ReindexDialogSearch;

internal sealed class ReindexDialogSearchCommandValidator : AbstractValidator<ReindexDialogSearchCommand>
{
    /// <summary>
    /// Configures validation rules for <see cref="ReindexDialogSearchCommand"/>.
    /// </summary>
    /// <remarks>
    /// Enforces that exactly one of the operation flags is specified: Full, Since, Resume, or StaleOnly.
    /// Validates optional numeric options when provided: BatchSize &gt; 0, Workers &gt; 0, ThrottleMs &gt;= 0, and WorkMemBytes &gt; 0.
    /// If the flag rule fails, the message "Specify exactly one of: --full OR --since &lt;ts&gt; OR --resume OR --stale-only." is used.
    /// </remarks>
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