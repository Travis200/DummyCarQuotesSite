using System;

namespace ExerciseApp.Service
{
    public sealed class QuoteCalculationResult
    {
        private QuoteCalculationResult(bool isSuccess, Guid? quoteId, decimal? quote, string errorMessage)
        {
            IsSuccess = isSuccess;
            QuoteId = quoteId;
            Quote = quote;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public Guid? QuoteId { get; }
        public decimal? Quote { get; }
        public string ErrorMessage { get; }

        public static QuoteCalculationResult Success(Guid quoteId, decimal quote)
        {
            return new QuoteCalculationResult(true, quoteId, quote, null);
        }

        public static QuoteCalculationResult Failure(string errorMessage)
        {
            return new QuoteCalculationResult(false, null, null, errorMessage);
        }
    }
}
