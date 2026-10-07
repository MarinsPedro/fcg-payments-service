using FiapCloudGames.Payments.Infrastructure.Persistence;
using FiapCloudGames.Payments.Migrations;
using FiapCloudGames.Payments.Migrations.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = MigrationConnection.Resolve(builder.Configuration);

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly.FullName)));

builder.Services.AddScoped<MigrationRunner>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();

await runner.RunAsync(CancellationToken.None);

public partial class Program;