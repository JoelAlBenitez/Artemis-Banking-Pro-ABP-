using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts
{
    public sealed class SavingsAccountsRepository :
        GenericRepository<SavingsAccount, int>,
        ISavingsAccountsRepository
    {
        public SavingsAccountsRepository(DbContextArtemisBanking context) : base(context) { }

        //Último resguardo de la unicidad del número de 9 dígitos dentro de las cuentas.
        //La verificación cruzada contra los préstamos vive en el generador del número.
        public override async Task<SavingsAccount> AddAsync(SavingsAccount entity)
        {
            var accountNumberInUse = await ExistsAccountNumberAsync(entity.AccountNumber);
            if (accountNumberInUse)
            {
                throw new InvalidOperationException(
                    "El número generado para la cuenta de ahorro ya se encuentra registrado.");
            }

            return await base.AddAsync(entity);
        }

        public async Task<bool> ExistsAccountNumberAsync(string accountNumber)
            => await ExistElementByConsult(account => account.AccountNumber == accountNumber);

        public async Task<SavingsAccount?> GetActivePrimaryAccountAsync(
            string customerId, bool asNoTracking = true)
        {
            IQueryable<SavingsAccount> query = _context.SavingsAccounts;

            if (asNoTracking) query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(account =>
                account.CustomerId == customerId &&
                account.AccountType == SavingsAccountType.Principal &&
                account.Status == SavingsAccountStatus.Activa);
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
