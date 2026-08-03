using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Transactions
{
    public sealed class TransactionRepository :
        GenericRepository<Transaction, int>,
        ITransactionRepository
    {
        public TransactionRepository(DbContextArtemisBanking context) : base(context) { }

        public async Task<int> GetTotalHistoricalAsync()
        {
            return await _context.Transactions.CountAsync();
        }

        public async Task<int> GetTotalTodayAsync()
        {
            var today = DateTimeOffset.UtcNow.Date;
            return await _context.Transactions.CountAsync(t => t.CreatedAt.Date == today);
        }

        public async Task<IReadOnlyList<Transaction>> GetPaymentsAsync(ChannelPayment? channel = null, DateTimeOffset? date = null)
        {
            IQueryable<Transaction> query = _context.Transactions
                .AsNoTracking()
                .Where(t => (t.OperationType == OperationType.PagoTarjeta || t.OperationType == OperationType.PagoPrestamo) 
                            && t.Status == TransactionStatus.Aprobada);

            if (channel.HasValue)
            {
                query = query.Where(t => t.Channel == channel.Value);
            }

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                query = query.Where(t => t.CreatedAt.Date == targetDate);
            }

            return await query.ToListAsync();
        }
    }
}
