using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Store.Core.Contracts.Messaging;
using Store.Core.Events;
using Store.Infrastructure.Messaging.Inbox;
using Store.Infrastructure.Messaging.Outbox;

namespace Store.Infrastructure.Messaging;

public sealed class InboxConsumerBuilder(IServiceCollection services)
{
    internal Dictionary<string, IEventBinding> Bindings { get; } = [];

    /// <summary>Esta cola procesa eventos TEvent con THandler (resuelto por scope, por mensaje).</summary>
    public InboxConsumerBuilder Handle<TEvent, THandler>()
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
        Bindings[TEvent.EventType] = new EventBinding<TEvent>();
        return this;
    }
}

public static class MessagingServiceCollectionExtensions
{
    /// <summary>Conexión, publicador con confirms y topología. La config se valida acá, al arrancar.</summary>
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(RabbitMqSettings.From(configuration));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqPublisher>();

        services.AddHostedService<RabbitMqTopologyInitializer>();
        return services;
    }

    /// <summary>Publica en RabbitMQ lo que haya en la tabla outbox del contexto indicado.</summary>
    public static IServiceCollection AddOutboxPublisher<TContext>(this IServiceCollection services)
        where TContext : DbContext =>
        services.AddHostedService<OutboxPublisher<TContext>>();

    /// <summary>Consume una cola con Inbox idempotente, reintentos y DLQ.</summary>
    public static IServiceCollection AddInboxConsumer<TContext>(
        this IServiceCollection services, string queue, Action<InboxConsumerBuilder> configure)
        where TContext : DbContext
    {
        var builder = new InboxConsumerBuilder(services);
        configure(builder);

        var options = new InboxConsumerOptions { Queue = queue };
        services.TryAddSingleton<InboxProcessor<TContext>>();
        return services.AddSingleton<IHostedService>(sp => new InboxConsumer<TContext>(
            options,
            builder.Bindings,
            sp.GetRequiredService<RabbitMqConnection>(),
            sp.GetRequiredService<RabbitMqPublisher>(),
            sp.GetRequiredService<InboxProcessor<TContext>>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<InboxConsumer<TContext>>>()));
    }
}
