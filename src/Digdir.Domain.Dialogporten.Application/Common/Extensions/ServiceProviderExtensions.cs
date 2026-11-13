using Digdir.Domain.Dialogporten.Application.Common.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

public static class ServiceProviderExtensions
{
    extension(IServiceProvider services)
    {
        /// <summary>
        /// Creates a new scope that inherits the transaction time and application context metadata of the
        /// current scope. Use it when work has to run on its own scoped services (for example its own
        /// DbContext and transaction) while still being timestamped and handled as part of the current request.
        /// </summary>
        public AsyncServiceScope CreateInheritingAsyncScope()
        {
            var parentTransactionTime = services.GetRequiredService<ITransactionTime>();
            var parentApplicationContext = services.GetRequiredService<IApplicationContext>();

            var scope = services.CreateAsyncScope();
            var scopedTransactionTime = scope.ServiceProvider.GetRequiredService<ITransactionTime>();
            var scopedApplicationContext = scope.ServiceProvider.GetRequiredService<IApplicationContext>();

            InheritTransactionTime(scopedTransactionTime, parentTransactionTime);
            foreach (var (key, value) in parentApplicationContext.Metadata)
            {
                scopedApplicationContext.AddMetadata(key, value);
            }

            return scope;
        }
    }

    private static void InheritTransactionTime(ITransactionTime target, ITransactionTime source)
    {
        if (target is TransactionTime transactionTime)
        {
            transactionTime.Inherit(source.Value);
            return;
        }

        if (target.Value != source.Value)
        {
            throw new InvalidOperationException(
                $"Unable to inherit the transaction time into {target.GetType().Name}.");
        }
    }
}
