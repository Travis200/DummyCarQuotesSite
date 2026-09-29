using System;
using System.Threading.Tasks;
using ExerciseApp.Data;
using ExerciseApp.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExerciseApp.Tests
{
    public class SqliteQuoteRepositoryTests
    {
        [Fact]
        public async Task SavedQuoteCanBeRetrievedFromANewDbContext()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<QuoteDbContext>()
                .UseSqlite(connection)
                .Options;
            var quote = new QuoteRecord
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                DateOfBirth = new DateTime(2000, 05, 01),
                Make = "BMW",
                Model = "X5",
                InsuranceType = InsuranceType.FullyComprehensive,
                Premium = 500m
            };

            await using (var writeContext = new QuoteDbContext(options))
            {
                await writeContext.Database.EnsureCreatedAsync();
                var repository = new SqliteQuoteRepository(writeContext);
                await repository.AddAsync(quote);
            }

            await using (var readContext = new QuoteDbContext(options))
            {
                var repository = new SqliteQuoteRepository(readContext);
                var savedQuote = await repository.GetByIdAsync(quote.Id);

                Assert.NotNull(savedQuote);
                Assert.Equal(quote.Id, savedQuote.Id);
                Assert.Equal(new DateTime(2000, 05, 01), savedQuote.DateOfBirth);
                Assert.Equal("BMW", savedQuote.Make);
                Assert.Equal("X5", savedQuote.Model);
                Assert.Equal(InsuranceType.FullyComprehensive, savedQuote.InsuranceType);
                Assert.Equal(500m, savedQuote.Premium);
            }
        }
    }
}
