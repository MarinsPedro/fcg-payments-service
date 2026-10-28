using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Payments.Api.Configuration;
using FiapCloudGames.Payments.Infrastructure;
using FiapCloudGames.Payments.Presentation;
using FiapCloudGames.Payments.Infrastructure.Persistence;
using MassTransit;
using FiapCloudGames.Payments.Contracts.Events.V1;

var builder = WebApplication.CreateBuilder(args);
builder.AddFiapServiceDefaults();
builder.Services.AddFiapApiDefaults();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<PaymentsDbContext>(outbox =>
    {
        outbox.UsePostgres();
        outbox.UseBusOutbox();

        outbox.QueryDelay = TimeSpan.FromSeconds(5);
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

        cfg.Host(
            rabbitHost,
            rabbitPort,
            "/",
            host =>
            {
                host.Username(rabbitUsername);
                host.Password(rabbitPassword);
            });
        
            
        var paymentsExchange = "fcg.payments.events";

        // JSON puro, compatível com consumidores legados.
        cfg.AddRawJsonSerializer();

        // PAYMENT PAID
        cfg.Message<PaymentPaidEventV1>(message =>
        {
            message.SetEntityName(paymentsExchange);
        });

        cfg.Send<PaymentPaidEventV1>(send =>
        {
            send.UseRoutingKeyFormatter(
                _ => "payments.paid");

            send.UseSerializer("application/json");
        });

        cfg.Publish<PaymentPaidEventV1>(publish =>
        {
            publish.ExchangeType = "topic";
        });

        // PAYMENT FAILED
        cfg.Message<PaymentFailedEventV1>(message =>
        {
            message.SetEntityName(paymentsExchange);
        });

        cfg.Send<PaymentFailedEventV1>(send =>
        {
            send.UseRoutingKeyFormatter(
                _ => "payments.failed");

            send.UseSerializer("application/json");
        });

        cfg.Publish<PaymentFailedEventV1>(publish =>
        {
            publish.ExchangeType = "topic";
        });

        // Provisiona a topologia de publicação.
        cfg.DeployPublishTopology = true;
    });

    
});

builder.Services.AddPaymentsPresentation();
builder.Services.AddPaymentsOpenApi();


var app = builder.Build();
app.UseFiapApiDefaults();
app.UsePaymentsOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapFiapHealthEndpoints();
app.MapControllers();
await app.RunAsync();

public partial class Program;
