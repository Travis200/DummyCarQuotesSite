using ExerciseApp.Model;

namespace ExerciseApp.Service
{
    public sealed class ThirdPartyOnlyQuoteStrategy : IQuoteStrategy
    {
        public InsuranceType Type => InsuranceType.ThirdPartyOnly;

        public decimal Calculate(QuoteRequest request)
        {
            if (request.Make == "Ford")
                return 180;
            if (request.Make == "Audi")
                return 250;

            return 300;
        }
    }
}
