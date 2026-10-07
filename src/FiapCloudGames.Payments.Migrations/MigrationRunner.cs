using FiapCloudGames.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Payments.Migrations;

internal sealed class MigrationRunner(
    PaymentsDbContext dbContext,
    ILogger<MigrationRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var pending = (await dbContext.Database
            .GetPendingMigrationsAsync(cancellationToken))
            .ToArray();

        if (pending.Length == 0)
        {
            logger.LogInformation("Nenhuma migration pendente de Payments.");
            return;
        }

        logger.LogInformation(
            "Aplicando {Count} migration(s) de Payments: {Migrations}.",
            pending.Length,
            string.Join(", ", pending));

        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation(
            "Migrations de Payments aplicadas com sucesso.");
    }
}