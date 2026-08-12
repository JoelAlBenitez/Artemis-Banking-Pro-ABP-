using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.Commerces
{
    public sealed class CommercePaymentConfiguration : IEntityTypeConfiguration<CommercePayment>
    {
        public void Configure(EntityTypeBuilder<CommercePayment> builder)
        {
            builder.ToTable("CommercePayments");
            builder.HasKey(payment => payment.Id);

            builder.Property(payment => payment.CardLastFourDigits)
                .IsRequired()
                .HasMaxLength(DomainConstants.LastFourDigitsLength)
                .IsUnicode(false);

            builder.Property(payment => payment.Amount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(payment => payment.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(payment => payment.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(payment => payment.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasOne(payment => payment.Commerce)
                .WithMany(commerce => commerce.Payments)
                .HasForeignKey(payment => payment.CommerceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(payment => payment.CreditCard)
                .WithMany()
                .HasForeignKey(payment => payment.CreditCardId)
                .OnDelete(DeleteBehavior.Restrict);

            //Listado de transacciones del comercio, de la más reciente a la más antigua
            builder.HasIndex(payment => new { payment.CommerceId, payment.CreatedAt })
                .HasDatabaseName("IX_CommercePayments_CommerceId_CreatedAt");
        }
    }
}
