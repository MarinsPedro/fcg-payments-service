using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.Payments.Application;
using FiapCloudGames.Payments.Application.Abstractions.Payments;
using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Domain.Outbox;
using FiapCloudGames.Payments.Domain.Payments;
using FiapCloudGames.Payments.Infrastructure.Health;
using FiapCloudGames.Payments.Infrastructure.Payments;
using FiapCloudGames.Payments.Infrastructure.Persistence;
using FiapCloudGames.Payments.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "A connection string 'Database' não foi configurada.");

        services.AddDbContext<PaymentsDbContext>(
            options => options.UseNpgsql(connectionString));

        services.AddScoped<IPaymentsUnitOfWork>(
            provider => provider.GetRequiredService<PaymentsDbContext>());

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }

    public static IServiceCollection AddPaymentsProcessing(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddPaymentsApplication();

    services.AddPaymentsPersistence(configuration);

    services.AddSingleton<IPaymentProvider, FakePaymentProvider>();

    return services;
}

   public static IServiceCollection AddPaymentsInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddPaymentsProcessing(configuration);

    services.AddFiapServiceAuthentication(configuration);

    services
        .AddHealthChecks()
        .AddCheck<PaymentsDatabaseHealthCheck>(
            "payments-database",
            tags: ["ready"]);

    return services;
}

    
}