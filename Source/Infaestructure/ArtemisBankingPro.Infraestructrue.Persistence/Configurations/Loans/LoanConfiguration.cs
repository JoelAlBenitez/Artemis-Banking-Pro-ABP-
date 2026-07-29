using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Configurations.Loans
{
    public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            // Revisiones -> Use constantes definidas en common / domain para los valores de preciones y longitudes
            //para evitar hardcode y a la vez manejar de forma directa los elementos de preciones para futuras nuevas integraciones
            // o escalamientos
            builder.ToTable("Loans");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.LoanNumber)
                .IsRequired()
                .HasMaxLength(DomainConstants.LoanNumberLength)
                .IsUnicode(false);

            builder.HasIndex(l => l.LoanNumber).IsUnique();

            builder.Property(l => l.CustomerId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(l => l.ApprovedCapital)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(l => l.MonthlyInstallment)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(l => l.TotalPayable)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(l => l.PendingAmount)
                .HasPrecision(DomainConstants.MoneyPrecision, DomainConstants.MoneyScale);

            builder.Property(l => l.AnnualInterestRate)
                .HasPrecision(DomainConstants.RatePrecision, DomainConstants.RateScale);

            builder.Property(l => l.termMonths)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(l => l.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(l => l.CreateByUserId)
                .IsRequired()
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.Property(l => l.LastModifiedByIdUser)
                .HasMaxLength(DomainConstants.IdentityUserIdLength);

            builder.HasIndex(l => l.CustomerId)
                .IsUnique()
                .HasFilter($"[Status] = {(int)LoanStatus.Activo}");

            builder.HasMany(l => l.loanInstallments)
                .WithOne(i => i.Loan)
                .HasForeignKey(i => i.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
