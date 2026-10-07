using FiapCloudGames.Payments.Infrastructure.Persistence;
using FiapCloudGames.Payments.Migrations.Configuration;
using Microsoft.EntityFrameworkCore.Design;

namespace FiapCloudGames.Payments.Migrations.DesignTime;

public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("PAYMENTS_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=fcg_payments;Username=fcg_payments_app;Password=postgres";
        return new PaymentsDbContext(MigrationDbContextOptions.Create(connection));
    }
}
