using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExerciseApp.Data;
using ExerciseApp.Model;
using ExerciseApp.Service;
using Xunit;

namespace ExerciseApp.Tests
{
    public class QuoteServiceTests
    {
        [Fact]
        public async Task WhenDetailsAreProvided_AQuoteIsProducedAndSaved()
        {
            var (service, repository) = CreateQuoteService();
            var quoteResult = await service.PerformQuoteAsync(CreateRequest(new DateTime(2000, 05, 01)));

            Assert.True(quoteResult.IsSuccess);
            Assert.Equal(200m, quoteResult.Quote.Value);
            Assert.NotNull(quoteResult.QuoteId);
            Assert.Null(quoteResult.ErrorMessage);
            var savedQuote = Assert.Single(repository.Quotes);
            Assert.Equal(new DateTime(2000, 05, 01), savedQuote.DateOfBirth);
        }

        [Theory]
        [InlineData(-16, "You must be at least 17 years old to receive a quote.")]
        [InlineData(-81, "You must be 80 years old or younger to receive a quote.")]
        public async Task WhenApplicantIsOutsideAgeRange_QuoteIsRejected(int ageOffset, string expectedError)
        {
            var (service, repository) = CreateQuoteService();
            var request = CreateRequest(DateTime.Today.AddYears(ageOffset));

            var result = await service.PerformQuoteAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Quote);
            Assert.Null(result.QuoteId);
            Assert.Equal(expectedError, result.ErrorMessage);
            Assert.Empty(repository.Quotes);
        }

        [Theory]
        [InlineData(-17)]
        [InlineData(-80)]
        public async Task WhenApplicantIsAtAgeBoundary_QuoteIsProduced(int ageOffset)
        {
            var request = CreateRequest(DateTime.Today.AddYears(ageOffset));

            var (service, _) = CreateQuoteService();
            var result = await service.PerformQuoteAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Equal(200m, result.Quote.Value);
        }

        [Fact]
        public async Task WhenDateOfBirthIsMissing_ReturnsReason()
        {
            var (service, _) = CreateQuoteService();
            var result = await service.PerformQuoteAsync(CreateRequest(null));

            Assert.False(result.IsSuccess);
            Assert.Equal("Date of birth is required to calculate a quote.", result.ErrorMessage);
        }

        [Fact]
        public async Task WhenInsuranceTypeIsMissing_ReturnsReason()
        {
            var request = CreateRequest(DateTime.Today.AddYears(-30));
            request.InsuranceType = null;

            var (service, _) = CreateQuoteService();
            var result = await service.PerformQuoteAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal("An insurance type is required to calculate a quote.", result.ErrorMessage);
        }

        [Fact]
        public async Task WhenInsuranceTypeHasNoRegisteredStrategy_ReturnsReason()
        {
            var request = CreateRequest(DateTime.Today.AddYears(-30));
            request.InsuranceType = (InsuranceType)999;

            var (service, _) = CreateQuoteService();
            var result = await service.PerformQuoteAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal("A quote is not available for the selected insurance type.", result.ErrorMessage);
        }

        [Theory]
        [InlineData("Porsche", "911")]
        [InlineData("BMW", "M3")]
        [InlineData("Ford", "X5")]
        public async Task WhenMakeAndModelAreNotSupported_ReturnsReasonAndDoesNotSave(string make, string model)
        {
            var (service, repository) = CreateQuoteService();
            var request = CreateRequest(DateTime.Today.AddYears(-30), make, model);

            var result = await service.PerformQuoteAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Quote);
            Assert.Null(result.QuoteId);
            Assert.Equal($"A quote is unavailable for make '{make}' and model '{model}'.", result.ErrorMessage);
            Assert.Empty(repository.Quotes);
        }

        [Fact]
        public async Task WhenMakeAndModelDifferOnlyByCase_UsesCanonicalVehicleForPricing()
        {
            var (service, repository) = CreateQuoteService();
            var request = CreateRequest(DateTime.Today.AddYears(-30), "bmw", "x5");

            var result = await service.PerformQuoteAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Equal(500m, result.Quote.Value);
            var savedQuote = Assert.Single(repository.Quotes);
            Assert.Equal("BMW", savedQuote.Make);
            Assert.Equal("X5", savedQuote.Model);
        }

        private static (QuoteService Service, FakeQuoteRepository Repository) CreateQuoteService()
        {
            var repository = new FakeQuoteRepository();
            var service = new QuoteService(new IQuoteStrategy[]
            {
                new FullyComprehensiveQuoteStrategy(),
                new ThirdPartyFireAndTheftQuoteStrategy(),
                new ThirdPartyOnlyQuoteStrategy()
            }, repository);

            return (service, repository);
        }

        private static QuoteRequest CreateRequest(DateTime? dateOfBirth, string make = "Ford", string model = "Focus")
        {
            return new QuoteRequest
            {
                DateOfBirth = dateOfBirth,
                InsuranceType = InsuranceType.FullyComprehensive,
                Make = make,
                Model = model
            };
        }

        private sealed class FakeQuoteRepository : IQuoteRepository
        {
            private readonly Dictionary<Guid, QuoteRecord> _quotes = new Dictionary<Guid, QuoteRecord>();

            public IReadOnlyCollection<QuoteRecord> Quotes => _quotes.Values;

            public Task<QuoteRecord> AddAsync(QuoteRecord quote, CancellationToken cancellationToken = default)
            {
                _quotes.Add(quote.Id, quote);
                return Task.FromResult(quote);
            }

            public Task<QuoteRecord> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            {
                QuoteRecord quote;
                _quotes.TryGetValue(id, out quote);
                return Task.FromResult(quote);
            }

            public Task<IReadOnlyList<QuoteRecord>> GetLatestAsync(
                int count,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<QuoteRecord> quotes = _quotes.Values
                    .OrderByDescending(quote => quote.CreatedAtUtc)
                    .ThenByDescending(quote => quote.Id)
                    .Take(count)
                    .ToList();
                return Task.FromResult(quotes);
            }
        }
    }
}
