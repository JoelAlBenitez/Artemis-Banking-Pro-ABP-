using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.Loans
{
    public sealed class LoanPaymentConfiguration : IEntityTypeConfiguration<LoanPayment>
    {
        public void Configure(EntityTypeBuilder<LoanPayment> builder)
        {
            builder.ToTable("LoanPayments");
            builder.HasKey(p => p.Id);


            builder.Property(p => p.RequestedAmount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(p => p.EffectiveAmount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(p => p.Channel)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(p => p.PerformedByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(p => p.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(p => p.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasOne(p => p.Transaction)
                .WithMany()
                .HasForeignKey(p => p.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Loans)
                .WithMany()
                .HasForeignKey(p => p.LoandId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.loanInstallment)
                .WithMany()
                .HasForeignKey(p => p.LoanInstallmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
