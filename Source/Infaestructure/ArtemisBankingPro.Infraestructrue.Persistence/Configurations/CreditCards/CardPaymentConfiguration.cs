using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.CreditCards
{
    public sealed class CardPaymentConfiguration : IEntityTypeConfiguration<CardPayment>
    {
        public void Configure(EntityTypeBuilder<CardPayment> builder)
        {
            builder.ToTable("CardPayments");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.CreditCardId)
                .IsRequired();

            builder.Property(p => p.TransactionId)
                .IsRequired();

            builder.HasIndex(p => p.TransactionId).IsUnique();

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

            builder.HasOne(p => p.CreditCard)
                .WithMany()
                .HasForeignKey(p => p.CreditCardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Transaction)
                .WithMany()
                .HasForeignKey(p => p.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
