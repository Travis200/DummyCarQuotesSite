using Microsoft.EntityFrameworkCore;

namespace ExerciseApp.Data
{
    public class QuoteDbContext : DbContext
    {
        public QuoteDbContext(DbContextOptions<QuoteDbContext> options)
            : base(options)
        {
        }

        public DbSet<QuoteRecord> Quotes => Set<QuoteRecord>();
    }
}
