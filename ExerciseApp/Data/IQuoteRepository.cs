using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExerciseApp.Data
{
    public interface IQuoteRepository
    {
        Task<QuoteRecord> AddAsync(QuoteRecord quote, CancellationToken cancellationToken = default);
        Task<QuoteRecord> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
