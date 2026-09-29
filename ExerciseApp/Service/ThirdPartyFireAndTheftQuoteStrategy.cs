using ExerciseApp.Model;

namespace ExerciseApp.Service
{
    public sealed class ThirdPartyFireAndTheftQuoteStrategy : IQuoteStrategy
    {
        public InsuranceType Type => InsuranceType.ThirdPartyFireAndTheft;

        public decimal Calculate(QuoteRequest request)
        {
            if (request.Make == "Ford")
                return 180;
            if (request.Make == "BMW")
                return request.Model == "X5" ? 510 : 400;

            return 300;
        }
    }
}
