using FiapCloudGames.Payments.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapCloudGames.Payments.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProviderTransactionId).HasColumnName("provider_transaction_id").HasMaxLength(128);
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("ux_payments_order_id");
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("ux_payments_idempotency_key");
    }
}
