using Fraga.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fraga.Infrastructure.Data.Configurations.TransactionConfiguration;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("accounts", table =>
        {
            table.HasCheckConstraint(
            "CK_accounts_balance_non_negative",
            "\"Balance\" >= 0");
        });

        builder.ToTable("transactions", table =>
        {
            table.HasCheckConstraint(
                "CK_transactions_amount_positive",
                "\"Amount\" > 0");

            table.HasCheckConstraint(
                "CK_transactions_balance_after_non_negative",
                "\"BalanceAfter\" >= 0");
        });

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.EventId)
            .IsRequired();

        builder.HasIndex(transaction => new
        {
            transaction.AccountId,
            transaction.OccurredAt,
            transaction.Id
        })
        .IsDescending(false, true, true)
        .HasDatabaseName("IX_transactions_AccountId_OccurredAt_Id");

        builder.Property(transaction => transaction.AccountId)
            .IsRequired();

        builder.Property(transaction => transaction.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.OccurredAt)
            .IsRequired();
    }
}