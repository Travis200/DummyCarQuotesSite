using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExerciseApp.Controllers;
using ExerciseApp.Data;
using ExerciseApp.Model;
using ExerciseApp.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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

        [Fact]
        public async Task PreviousQuotesEndpointReturnsRequestedNumberNewestFirstAndDefaultsToFive()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<QuoteDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new QuoteDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
            var repository = new SqliteQuoteRepository(dbContext);
            var quotes = new List<QuoteRecord>();
            for (var index = 0; index < 7; index++)
            {
                var quote = new QuoteRecord
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = new DateTime(2026, 1, 1).AddMinutes(index),
                    DateOfBirth = new DateTime(2000, 1, 1),
                    Make = "BMW",
                    Model = "X5",
                    InsuranceType = InsuranceType.FullyComprehensive,
                    Premium = 500m
                };
                quotes.Add(quote);
                await repository.AddAsync(quote);
            }

            var quoteService = new QuoteService(new IQuoteStrategy[]
            {
                new FullyComprehensiveQuoteStrategy(),
                new ThirdPartyFireAndTheftQuoteStrategy(),
                new ThirdPartyOnlyQuoteStrategy()
            }, repository);
            var controller = new QuoteController(quoteService, NullLogger<QuoteController>.Instance);

            var requestedResult = await controller.GetPreviousQuotes(3);
            var requestedQuotes = Assert.IsAssignableFrom<IReadOnlyList<QuoteHistoryItem>>(
                Assert.IsType<OkObjectResult>(requestedResult.Result).Value);
            Assert.Equal(3, requestedQuotes.Count);
            Assert.Equal(quotes.Skip(4).Reverse().Select(quote => quote.Id),
                requestedQuotes.Select(quote => quote.QuoteId));

            var defaultResult = await controller.GetPreviousQuotes();
            var defaultQuotes = Assert.IsAssignableFrom<IReadOnlyList<QuoteHistoryItem>>(
                Assert.IsType<OkObjectResult>(defaultResult.Result).Value);
            Assert.Equal(5, defaultQuotes.Count);

            var invalidResult = await controller.GetPreviousQuotes(101);
            Assert.IsType<BadRequestObjectResult>(invalidResult.Result);
        }
    }
}
