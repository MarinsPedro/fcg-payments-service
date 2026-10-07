using FiapCloudGames.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiapCloudGames.Payments.Migrations.Migrations;

[DbContext(typeof(PaymentsDbContext))]
[Migration("20261006000100_InitialPayments")]
public sealed class InitialPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "payments");
        migrationBuilder.CreateTable(
            name: "payments",
            schema: "payments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                provider_transaction_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_payments", x => x.id));

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "payments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_outbox_messages", x => x.id));

        migrationBuilder.CreateIndex(name: "ux_payments_order_id", schema: "payments", table: "payments", column: "order_id", unique: true);
        migrationBuilder.CreateIndex(name: "ux_payments_idempotency_key", schema: "payments", table: "payments", column: "idempotency_key", unique: true);
        migrationBuilder.CreateIndex(name: "ix_outbox_pending", schema: "payments", table: "outbox_messages", columns: new[] { "processed_at_utc", "occurred_at_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "outbox_messages", schema: "payments");
        migrationBuilder.DropTable(name: "payments", schema: "payments");
    }
}
