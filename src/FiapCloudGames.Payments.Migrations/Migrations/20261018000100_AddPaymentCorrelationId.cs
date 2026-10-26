using FiapCloudGames.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiapCloudGames.Payments.Migrations.Migrations;

[DbContext(typeof(PaymentsDbContext))]
[Migration("20261018000100_AddPaymentCorrelationId")]
public sealed class AddPaymentCorrelationId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "correlation_id",
            schema: "payments",
            table: "payments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            defaultValue: string.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "correlation_id",
            schema: "payments",
            table: "payments");
    }
}
