using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.Payments.Presentation.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Payments.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsPresentation(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(DependencyInjection).Assembly);
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PaymentsPolicies.Create, policy => policy.RequireAuthenticatedUser().RequireServiceToken().RequireScopes("payments.create"));
            options.AddPolicy(PaymentsPolicies.Read, policy => policy.RequireAuthenticatedUser().RequireServiceToken().RequireScopes("payments.read"));
            options.AddPolicy(PaymentsPolicies.Confirm, policy => policy.RequireAuthenticatedUser().RequireServiceToken().RequireScopes("payments.confirm"));
            options.AddPolicy(PaymentsPolicies.Cancel, policy => policy.RequireAuthenticatedUser().RequireServiceToken().RequireScopes("payments.cancel"));
        });
        return services;
    }
}
