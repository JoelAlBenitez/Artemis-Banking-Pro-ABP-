using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.SavingsAccounts
{
    public sealed class SavingsAccountConfiguration : IEntityTypeConfiguration<SavingsAccount>
    {
        public void Configure(EntityTypeBuilder<SavingsAccount> builder)
        {
            builder.ToTable("SavingsAccounts");
            builder.HasKey(a => a.Id);

            //Texto y no numérico para no perder los ceros iniciales del número de 9 dígitos
            builder.Property(a => a.AccountNumber)
                .IsRequired()
                .HasMaxLength(DomainConstants.AccountNumberLength)
                .IsUnicode(false);

            builder.HasIndex(a => a.AccountNumber).IsUnique();

            builder.Property(a => a.CustomerId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(a => a.Balance)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(a => a.AccountType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(a => a.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(a => a.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(a => a.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

<<<<<<< HEAD
            builder.HasIndex(a => a.CustomerId);

            //Una sola cuenta principal activa por cliente
            builder.HasIndex(a => a.CustomerId)
=======
            //Búsqueda de todas las cuentas de un cliente
            builder.HasIndex(a => a.CustomerId)
                .HasDatabaseName("IX_SavingsAccounts_CustomerId");

            //Una sola cuenta principal activa por cliente. Va con nombre propio porque dos
            //HasIndex sin nombre sobre la misma propiedad reconfiguran el mismo índice en vez
            //de crear uno nuevo, y el filtrado terminaría reemplazando al de búsqueda.
            builder.HasIndex(a => a.CustomerId)
                .HasDatabaseName("UX_SavingsAccounts_ActivePrimaryPerCustomer")
>>>>>>> origin/development
                .IsUnique()
                .HasFilter($"[AccountType] = {(int)SavingsAccountType.Principal} " +
                           $"AND [Status] = {(int)SavingsAccountStatus.Activa}");

            builder.Ignore(a => a.IsPrimary);
            builder.Ignore(a => a.IsActive);

<<<<<<< HEAD
            //La relaci�n con el historial de transacciones se configura cuando el m�dulo
            //Cliente exponga la entidad Transaction (OnDelete Restrict, sin borrado f�sico).
=======
            //El extremo inverso del historial de transacciones lo configura
            //TransactionConfiguration (OnDelete Restrict, sin borrado físico).
>>>>>>> origin/development
        }
    }
}
