
using FiapCloudGames.Payments.Application.Features.Payments.Confirm;
using FiapCloudGames.Payments.Application.Features.Payments.Create;
using FiapCloudGames.Payments.Application.Features.Payments.Get;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Payments.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        services.AddScoped<CreatePaymentService>();
        services.AddScoped<GetPaymentService>();
        services.AddScoped<ConfirmPaymentService>();
        return services;
    }
}
