using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Loans
{
    public sealed class LoansRepository :
        GenericRepository<Loan, int>,
        ILoansRepository
    {
        private const string LoanNumberSequence = "LoanNumberSequence";

        public LoansRepository(DbContextArtemisBanking context) : base(context) { }

        //El rango de la secuencia arranca en 9 dígitos, así que el valor emitido ya tiene el
        //largo exigido. El PadLeft solo protege el formato de texto del contrato.
        //NEXT VALUE FOR debe ejecutarse como comando directo: SQL Server lo rechaza dentro de
        //una tabla derivada, que es en lo que se traduce una consulta LINQ sobre SqlQueryRaw.
        public async Task<string> GetNextLoanNumberAsync()
        {
            await _context.Database.OpenConnectionAsync();
            try
            {
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"SELECT NEXT VALUE FOR [{LoanNumberSequence}]";
                var result = await command.ExecuteScalarAsync();
                var nextValue = Convert.ToInt32(result);

                return nextValue
                    .ToString(CultureInfo.InvariantCulture)
                    .PadLeft(DomainConstants.LoanNumberLength, '0');
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        public async Task<PagedResult<Loan>> GetPagedLoansAsync(
            int page,
            int pageSize,
            LoanStatus? status,
            string? customerId)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, DomainConstants.MaxPageSize);

            IQueryable<Loan> query = _context.Loans
                .AsNoTracking()
                .Include(l => l.loanInstallments);

            if (status is not null) query = query.Where(l => l.Status == status);
            if (!string.IsNullOrWhiteSpace(customerId)) query = query.Where(l => l.CustomerId == customerId);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(l => l.Status)
                .ThenByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Loan>(items, page, pageSize, totalRecords);
        }


    }
}