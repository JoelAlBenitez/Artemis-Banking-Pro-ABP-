using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.Beneficiaries
{
    public sealed class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
    {
        public void Configure(EntityTypeBuilder<Beneficiary> builder)
        {
            builder.ToTable("Beneficiaries");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.OwnerClientId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(b => b.BeneficiarySavingsAccountId)
                .IsRequired();

            builder.Property(b => b.BeneficiaryAccountNumber)
                .IsRequired()
                .HasMaxLength(9)
                .IsUnicode(false);

            builder.Property(b => b.IsActive)
                .IsRequired();

            builder.Property(b => b.DeactivatedAt);

            builder.Property(b => b.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(b => b.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasOne(b => b.BeneficiarySavingsAccount)
                .WithMany()
                .HasForeignKey(b => b.BeneficiarySavingsAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
