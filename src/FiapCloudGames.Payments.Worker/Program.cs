using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Infrastructure;
using FiapCloudGames.Payments.Infrastructure.Persistence;
using FiapCloudGames.Payments.Worker;
using MassTransit;

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
        var rabbitHost =
            builder.Configuration["RabbitMq:Host"]
            ?? "localhost";

        var rabbitPort =
            ushort.TryParse(
                builder.Configuration["RabbitMq:Port"],
                out var port)
                ? port
                : (ushort)5672;

        var rabbitUsername =
            builder.Configuration["RabbitMq:Username"]
            ?? "guest";

        var rabbitPassword =
            builder.Configuration["RabbitMq:Password"]
            ?? "guest";

        var exchange =
            builder.Configuration["RabbitMq:Exchange"]
            ?? "fcg.events";

        cfg.Host(
            rabbitHost,
            rabbitPort,
            "/",
            host =>
            {
                host.Username(rabbitUsername);
                host.Password(rabbitPassword);
            });

        cfg.ReceiveEndpoint(
            "fcg.payments.payment-requested",
            endpoint =>
            {
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;

                // Compatibilidade com os eventos JSON atuais
                // publicados sem envelope MassTransit.
                endpoint.UseRawJsonDeserializer(
                    RawSerializerOptions.All,
                    isDefault: true);

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

                // Mantém compatibilidade com a topologia atual:
                // fcg.events -> payments.requested
                endpoint.Bind(
                    exchange,
                    binding =>
                    {
                        binding.ExchangeType = "topic";
                        binding.RoutingKey =
                            "payments.requested";
                    });

                endpoint.ConfigureConsumer
                    <PaymentRequestedConsumer>(context);
            });
    });
});

var host = builder.Build();

await host.RunAsync();