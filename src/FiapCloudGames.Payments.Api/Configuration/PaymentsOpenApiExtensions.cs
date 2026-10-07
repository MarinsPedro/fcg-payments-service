using Microsoft.OpenApi;

namespace FiapCloudGames.Payments.Api.Configuration;

internal static class PaymentsOpenApiExtensions
{
    public static IServiceCollection AddPaymentsOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fiap Cloud Games Payments API",
                Version = "v1",
                Description = "Serviço de pagamentos do Fiap Cloud Games."
            });
            options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Informe somente o JWT, sem o prefixo Bearer."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });
        });
        return services;
    }

    public static WebApplication UsePaymentsOpenApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fiap Cloud Games Payments API v1");
            options.EnablePersistAuthorization();
        });
        return app;
    }
}
