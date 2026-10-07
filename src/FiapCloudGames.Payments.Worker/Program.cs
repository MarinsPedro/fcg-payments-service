using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Infrastructure;
using FiapCloudGames.Payments.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddFiapServiceDefaults();

builder.Services.AddPaymentsProcessing(
    builder.Configuration);

builder.Services.AddScoped<MessageCorrelationContext>();

builder.Services.AddScoped<ICorrelationContext>(
    provider =>
        provider.GetRequiredService<MessageCorrelationContext>());

builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<PaymentRequestedConsumer>();

var host = builder.Build();

await host.RunAsync();