namespace Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;

public interface IOrderDefinition<TTarget>
{
    static abstract IOrderOptions<TTarget> Configure(IOrderOptionsBuilder<TTarget> options);
}
