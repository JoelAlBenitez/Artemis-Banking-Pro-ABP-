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

            builder.Property(c => c.Amount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(c => c.Origin)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(c => c.CommerceName)
                .IsRequired()
                .HasMaxLength(DomainConstants.CommerceNameLength);

            builder.Property(c => c.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(c => c.RejectionReason)
                .HasConversion<int?>();

            builder.Property(c => c.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(c => c.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasIndex(c => new { c.CreditCardId, c.CreatedAt });

            //La relación con el comercio se configurará cuando exista la entidad Commerce,
            //propia del módulo de Hermes Pay.
        }
    }
}
