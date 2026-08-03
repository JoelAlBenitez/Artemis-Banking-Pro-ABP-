using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.Transactions
{
    public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.SavingsAccountId)
                .IsRequired();

            builder.Property(t => t.Amount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(t => t.TransactionType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.OperationType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.Origin)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(t => t.Beneficiary)
                .HasMaxLength(150);

            builder.Property(t => t.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.RejectionReason)
                .HasMaxLength(500);

            builder.Property(t => t.PerformedByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(t => t.Channel)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.RelatedTransactionId);

            builder.Property(t => t.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(t => t.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasOne(t => t.SavingsAccount)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.SavingsAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.RelatedTransaction)
                .WithMany()
                .HasForeignKey(t => t.RelatedTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
