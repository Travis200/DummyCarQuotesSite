namespace ExerciseApp.Service
{
    public sealed class QuoteCalculationResult
    {
        private QuoteCalculationResult(bool isSuccess, decimal? quote, string errorMessage)
        {
            IsSuccess = isSuccess;
            Quote = quote;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public decimal? Quote { get; }
        public string ErrorMessage { get; }

        public static QuoteCalculationResult Success(decimal quote)
        {
            return new QuoteCalculationResult(true, quote, null);
        }

        public static QuoteCalculationResult Failure(string errorMessage)
        {
            return new QuoteCalculationResult(false, null, errorMessage);
        }
    }
}
