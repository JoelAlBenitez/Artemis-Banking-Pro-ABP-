using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts
{
    public sealed class SavingsAccountsRepository :
        GenericRepository<SavingsAccount, int>,
        ISavingsAccountsRepository
    {
        private const string AccountNumberSequence = "SavingsAccountNumberSequence";

        public SavingsAccountsRepository(DbContextArtemisBanking context) : base(context) { }

        //El rango de la secuencia arranca en 9 dígitos, así que el valor emitido ya tiene el
        //largo exigido. El PadLeft solo protege el formato de texto del contrato.
        public async Task<string> GetNextAccountNumberAsync()
        {
            await _context.Database.OpenConnectionAsync();
            try
            {
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"SELECT NEXT VALUE FOR [{AccountNumberSequence}]";
                var result = await command.ExecuteScalarAsync();
                var nextValue = Convert.ToInt32(result);

                return nextValue
                    .ToString(CultureInfo.InvariantCulture)
                    .PadLeft(DomainConstants.AccountNumberLength, '0');
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        public async Task<PagedResult<SavingsAccount>> GetPagedSavingsAccountsAsync(
            int page,
            int pageSize,
            SavingsAccountStatus? status,
            SavingsAccountType? accountType,
            string? customerId)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, DomainConstants.MaxPageSize);

            IQueryable<SavingsAccount> query = _context.SavingsAccounts.AsNoTracking();

            if (status is not null) query = query.Where(account => account.Status == status);
            if (accountType is not null) query = query.Where(account => account.AccountType == accountType);
            if (!string.IsNullOrWhiteSpace(customerId)) query = query.Where(account => account.CustomerId == customerId);

            var totalRecords = await query.CountAsync();

            //Sin filtro de estado las activas se muestran primero; dentro de cada grupo,
            //de la más reciente a la más antigua.
            query = status is null
                ? query.OrderBy(account => account.Status).ThenByDescending(account => account.CreatedAt)
                : query.OrderByDescending(account => account.CreatedAt);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<SavingsAccount>(items, page, pageSize, totalRecords);
        }
    }
}
