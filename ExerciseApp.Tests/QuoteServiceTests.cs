using System;
using ExerciseApp.Model;
using ExerciseApp.Service;
using Xunit;

namespace ExerciseApp.Tests
{
    public class QuoteServiceTests
    {
        [Fact]
        public void WhenDetailsAreProvided_AQuoteIsProduced()
        {
            var quoteResult = CreateQuoteService().PerformQuote(CreateRequest(new DateTime(2000, 05, 01)));

            Assert.True(quoteResult.IsSuccess);
            Assert.Equal(200m, quoteResult.Quote.Value);
            Assert.Null(quoteResult.ErrorMessage);
        }

        [Theory]
        [InlineData(-16, "You must be at least 17 years old to receive a quote.")]
        [InlineData(-81, "You must be 80 years old or younger to receive a quote.")]
        public void WhenApplicantIsOutsideAgeRange_QuoteIsRejected(int ageOffset, string expectedError)
        {
            var request = CreateRequest(DateTime.Today.AddYears(ageOffset));

            var result = CreateQuoteService().PerformQuote(request);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Quote);
            Assert.Equal(expectedError, result.ErrorMessage);
        }

        [Theory]
        [InlineData(-17)]
        [InlineData(-80)]
        public void WhenApplicantIsAtAgeBoundary_QuoteIsProduced(int ageOffset)
        {
            var request = CreateRequest(DateTime.Today.AddYears(ageOffset));

            var result = CreateQuoteService().PerformQuote(request);

            Assert.True(result.IsSuccess);
            Assert.Equal(200m, result.Quote.Value);
        }

        [Fact]
        public void WhenDateOfBirthIsMissing_ReturnsReason()
        {
            var result = CreateQuoteService().PerformQuote(CreateRequest(null));

            Assert.False(result.IsSuccess);
            Assert.Equal("Date of birth is required to calculate a quote.", result.ErrorMessage);
        }

        [Fact]
        public void WhenInsuranceTypeIsMissing_ReturnsReason()
        {
            var request = CreateRequest(DateTime.Today.AddYears(-30));
            request.InsuranceType = null;

            var result = CreateQuoteService().PerformQuote(request);

            Assert.False(result.IsSuccess);
            Assert.Equal("An insurance type is required to calculate a quote.", result.ErrorMessage);
        }

        [Fact]
        public void WhenInsuranceTypeHasNoRegisteredStrategy_ReturnsReason()
        {
            var request = CreateRequest(DateTime.Today.AddYears(-30));
            request.InsuranceType = (InsuranceType)999;

            var result = CreateQuoteService().PerformQuote(request);

            Assert.False(result.IsSuccess);
            Assert.Equal("A quote is not available for the selected insurance type.", result.ErrorMessage);
        }

        private static QuoteService CreateQuoteService()
        {
            return new QuoteService(new IQuoteStrategy[]
            {
                new FullyComprehensiveQuoteStrategy(),
                new ThirdPartyFireAndTheftQuoteStrategy(),
                new ThirdPartyOnlyQuoteStrategy()
            });
        }

        private static QuoteRequest CreateRequest(DateTime? dateOfBirth)
        {
            return new QuoteRequest
            {
                DateOfBirth = dateOfBirth,
                InsuranceType = InsuranceType.FullyComprehensive,
                Make = "Ford",
                Model = "Focus"
            };
        }
    }
}
