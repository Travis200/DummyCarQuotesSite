using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExerciseApp.Data
{
    public class SqliteQuoteRepository : IQuoteRepository
    {
        private readonly QuoteDbContext _dbContext;

        public SqliteQuoteRepository(QuoteDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<QuoteRecord> AddAsync(QuoteRecord quote, CancellationToken cancellationToken = default)
        {
            await _dbContext.Quotes.AddAsync(quote, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return quote;
        }

        public Task<QuoteRecord> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.Quotes
                .AsNoTracking()
                .SingleOrDefaultAsync(quote => quote.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<QuoteRecord>> GetLatestAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Quotes
                .AsNoTracking()
                .OrderByDescending(quote => quote.CreatedAtUtc)
                .ThenByDescending(quote => quote.Id)
                .Take(count)
                .ToListAsync(cancellationToken);
        }
    }
}
