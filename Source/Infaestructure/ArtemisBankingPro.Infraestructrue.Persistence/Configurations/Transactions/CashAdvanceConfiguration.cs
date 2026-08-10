using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.Transactions
{
    public sealed class CashAdvanceConfiguration : IEntityTypeConfiguration<CashAdvance>
    {
        public void Configure(EntityTypeBuilder<CashAdvance> builder)
        {
            builder.ToTable("CashAdvances");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.CreditCardId)
                .IsRequired();

            builder.Property(c => c.SavingsAccountId)
                .IsRequired();

            builder.Property(c => c.RequestedAmount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.InterestRate)
                .HasPrecision(DomainConstants.RatePrecision, DomainConstants.RateScale);

            builder.Property(c => c.InterestAmount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.TotalCharged)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.CardConsumptionId)
                .IsRequired();

            builder.Property(c => c.TransactionId)
                .IsRequired();

            builder.Property(c => c.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(c => c.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasOne(c => c.CreditCard)
                .WithMany()
                .HasForeignKey(c => c.CreditCardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.SavingsAccount)
                .WithMany()
                .HasForeignKey(c => c.SavingsAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.CardConsumption)
                .WithMany()
                .HasForeignKey(c => c.CardConsumptionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Transaction)
                .WithMany()
                .HasForeignKey(c => c.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
