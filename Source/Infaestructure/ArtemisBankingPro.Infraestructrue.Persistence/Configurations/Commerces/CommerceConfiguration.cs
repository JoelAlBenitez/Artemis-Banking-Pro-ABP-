using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.Commerces
{
    public sealed class CommerceConfiguration : IEntityTypeConfiguration<Commerce>
    {
        public void Configure(EntityTypeBuilder<Commerce> builder)
        {
            builder.ToTable("Commerces");
            builder.HasKey(commerce => commerce.Id);

            builder.Property(commerce => commerce.Name)
                .IsRequired()
                .HasMaxLength(DomainConstants.CommerceNameLength);

            builder.Property(commerce => commerce.Description)
                .HasMaxLength(500);

            builder.Property(commerce => commerce.Email)
                .IsRequired()
                .HasMaxLength(160);

            builder.Property(commerce => commerce.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(commerce => commerce.Rnc)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false);

            builder.Property(commerce => commerce.Status)
                .HasConversion<int>()
                .IsRequired();

            //Referencia lógica al usuario de Identity: sin FK física entre contextos
            builder.Property(commerce => commerce.AssociatedUserId)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(commerce => commerce.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(commerce => commerce.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasIndex(commerce => commerce.Rnc).IsUnique();
            builder.HasIndex(commerce => commerce.Email).IsUnique();

            //Un comercio admite un solo usuario asociado. El filtro deja fuera los comercios
            //que todavía no tienen usuario, que sí pueden repetir el valor nulo.
            builder.HasIndex(commerce => commerce.AssociatedUserId)
                .HasDatabaseName("UX_Commerces_AssociatedUserId")
                .IsUnique()
                .HasFilter("[AssociatedUserId] IS NOT NULL");

            builder.Ignore(commerce => commerce.IsActive);
            builder.Ignore(commerce => commerce.HasAssociatedUser);
        }
    }
}
