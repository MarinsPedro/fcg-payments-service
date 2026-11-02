using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Infrastructure;
using FiapCloudGames.Payments.Infrastructure.Messaging;
using FiapCloudGames.Payments.Infrastructure.Persistence;
using FiapCloudGames.Payments.Worker;
using MassTransit;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.AddFiapServiceDefaults();

builder.Services.AddPaymentsProcessing(
    builder.Configuration);

builder.Services.AddScoped<MessageCorrelationContext>();

builder.Services.AddScoped<ICorrelationContext>(
    provider =>
        provider.GetRequiredService<MessageCorrelationContext>());

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentRequestedConsumer>();

    x.AddEntityFrameworkOutbox<PaymentsDbContext>(outbox =>
    {
        outbox.UsePostgres();
        outbox.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var options = context
            .GetRequiredService<IOptions<RabbitMqOptions>>()
            .Value;

        RabbitMqTopologyConfigurator.Configure(
            cfg,
            options.Topology.Exchanges);

        cfg.Host(
            options.Host,
            options.Port,
            options.VirtualHost,
            host =>
            {
                host.Username(options.Username);
                host.Password(options.Password);
            });

        cfg.ReceiveEndpoint(
            options.Topology.Queues.PaymentRequested,
            endpoint =>
            {
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;

                endpoint.UseMessageRetry(retry =>
                {
                    retry.Ignore<AppException>();

                    retry.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(3),
                        TimeSpan.FromSeconds(5));
                });

                endpoint.UseEntityFrameworkOutbox
                    <PaymentsDbContext>(context);

                endpoint.ConfigureConsumer
                    <PaymentRequestedConsumer>(context);
            });
    });
});

var host = builder.Build();

await host.RunAsync();
