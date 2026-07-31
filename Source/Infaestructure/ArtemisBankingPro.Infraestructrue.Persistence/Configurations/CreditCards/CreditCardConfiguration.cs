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
                .HasMaxLength(16)
                .IsUnicode(false);

            builder.HasIndex(c => c.CardNumber).IsUnique();

            builder.Property(c => c.CvcHash)
                .IsRequired();

            builder.Property(c => c.ExpirationDate)
                .IsRequired()
                .HasMaxLength(5)
                .IsUnicode(false);

            builder.Property(c => c.ClientId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(c => c.CreditLimit)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.OwedAmount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(c => c.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(c => c.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasMany(c => c.Consumptions)
                .WithOne(con => con.CreditCard)
                .HasForeignKey(con => con.CreditCardId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
