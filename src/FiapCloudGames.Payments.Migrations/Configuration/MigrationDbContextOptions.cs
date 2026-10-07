using FiapCloudGames.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Payments.Migrations.Configuration;

internal static class MigrationDbContextOptions
{
    public static DbContextOptions<PaymentsDbContext> Create(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<PaymentsDbContext>();
        builder.UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(Program).Assembly.FullName));
        return builder.Options;
    }
}
