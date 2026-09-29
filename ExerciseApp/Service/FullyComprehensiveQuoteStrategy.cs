using ExerciseApp.Model;

namespace ExerciseApp.Service
{
    public sealed class FullyComprehensiveQuoteStrategy : IQuoteStrategy
    {
        public InsuranceType Type => InsuranceType.FullyComprehensive;

        public decimal Calculate(QuoteRequest request)
        {
            if (request.Make == "Ford")
                return 200;
            if (request.Make == "BMW")
                return request.Model == "X5" ? 500 : 400;

            return 300;
        }
    }
}
