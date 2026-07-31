using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.CreditCards
{
    public sealed class CardConsumptionConfiguration : IEntityTypeConfiguration<CardConsumption>
    {
        public void Configure(EntityTypeBuilder<CardConsumption> builder)
        {
            builder.ToTable("CardConsumptions");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.CreditCardId)
                .IsRequired();

            builder.Property(c => c.Amount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.CommerceName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(c => c.RejectionReason)
                .HasMaxLength(500);

            builder.Property(c => c.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(c => c.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasOne(c => c.CreditCard)
                .WithMany(con => con.Consumptions)
                .HasForeignKey(c => c.CreditCardId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
