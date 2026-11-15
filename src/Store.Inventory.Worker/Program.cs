using Store.Core.Events;
using Store.Core.UseCases.Inventory;
using Store.Infrastructure.Messaging;
using Store.Infrastructure.Persistence;
using Store.Infrastructure.Persistence.Inventory;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInventoryPersistence(builder.Configuration);

builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddOutboxPublisher<InventoryDbContext>();
builder.Services.AddInboxConsumer<InventoryDbContext>(
    RabbitMqTopology.OrderCreatedQueue,
    consumer => consumer.Handle<OrderCreated, ReserveStockHandler>());

var host = builder.Build();

await host.Services.InitializeDatabaseAsync<InventoryDbContext>();

await host.RunAsync();
