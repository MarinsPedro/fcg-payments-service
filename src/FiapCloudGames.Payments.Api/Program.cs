using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Payments.Api.Configuration;
using FiapCloudGames.Payments.Infrastructure;
using FiapCloudGames.Payments.Infrastructure.Messaging;
using FiapCloudGames.Payments.Infrastructure.Persistence;
using FiapCloudGames.Payments.Presentation;
using MassTransit;
using Microsoft.Extensions.Options;

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
