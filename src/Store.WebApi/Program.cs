using System.Text.Json.Serialization;
using Store.Core.Events;
using Store.Core.UseCases.Orders;
using Store.Core.Validation.Orders;
using Store.Infrastructure.Messaging;
using Store.Infrastructure.Persistence;
using Store.Infrastructure.Persistence.Orders;
using Store.WebApi.Health;
using Store.WebApi.Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);

builder.Services.AddOrdersPersistence(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddOutboxPublisher<OrdersDbContext>();

builder.Services.AddInboxConsumer<OrdersDbContext>(
    RabbitMqTopology.StockResultsQueue,
    consumer => consumer
        .Handle<StockReserved, ConfirmOrderHandler>()
        .Handle<StockRejected, RejectOrderHandler>());

builder.Services.AddScoped<CreateOrderValidator>();
builder.Services.AddScoped<CreateOrderHandler>();

builder.Services.AddHealthChecks()
    .AddCheck<CatalogReadinessCheck>("catalog", tags: ["ready"]);

var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();

await app.Services.InitializeDatabaseAsync<OrdersDbContext>(app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (allowedOrigins.Length > 0)
{
    app.UseCors();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapOrderEndpoints();

// Liveness: el proceso responde (no toca la base). Readiness: puede atender pedidos.
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = r => r.Tags.Contains("ready") });

await app.RunAsync();
