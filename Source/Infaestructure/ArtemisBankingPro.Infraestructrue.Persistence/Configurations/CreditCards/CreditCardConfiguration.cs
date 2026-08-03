using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.CreditCards
{
    public sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
    {
        public void Configure(EntityTypeBuilder<CreditCard> builder)
        {
            builder.ToTable("CreditCards");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.CardNumber)
                .IsRequired()
                .HasMaxLength(DomainConstants.CardNumberLength)
                .IsUnicode(false);

            builder.HasIndex(c => c.CardNumber).IsUnique();

            builder.Property(c => c.LastFourDigits)
                .IsRequired()
                .HasMaxLength(DomainConstants.LastFourDigitsLength)
                .IsUnicode(false);

            builder.Property(c => c.CustomerId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(c => c.CreditLimit)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.OwedAmount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.ExpirationDate)
                .IsRequired();

            builder.Property(c => c.CvcHash)
                .IsRequired()
                .HasMaxLength(DomainConstants.CvcHashLength)
                .IsUnicode(false);

            builder.Property(c => c.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(c => c.AssignedByAdminId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(c => c.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(c => c.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasIndex(c => c.CustomerId);

            builder.Ignore(c => c.AvailableCredit);
            builder.Ignore(c => c.IsExpired);

            builder.HasMany(c => c.cardConsumptions)
                .WithOne(consumption => consumption.CreditCard)
                .HasForeignKey(consumption => consumption.CreditCardId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
