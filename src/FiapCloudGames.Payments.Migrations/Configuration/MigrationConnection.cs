using Microsoft.Extensions.Configuration;

namespace FiapCloudGames.Payments.Migrations.Configuration;

internal static class MigrationConnection
{
    public static string Resolve(IConfiguration configuration) =>
        Environment.GetEnvironmentVariable("PAYMENTS_MIGRATIONS_CONNECTION")
        ?? configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException(
            "Configure PAYMENTS_MIGRATIONS_CONNECTION ou ConnectionStrings:Database.");
}