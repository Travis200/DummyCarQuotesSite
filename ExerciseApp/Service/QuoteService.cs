using ExerciseApp.Model;
using System;
using System.Collections.Generic;

namespace ExerciseApp.Service
{
    public class QuoteService
    {
        private readonly IReadOnlyDictionary<InsuranceType, IQuoteStrategy> _quoteStrategies;

        public QuoteService(IEnumerable<IQuoteStrategy> quoteStrategies)
        {
            var strategiesByType = new Dictionary<InsuranceType, IQuoteStrategy>();
            foreach (var quoteStrategy in quoteStrategies)
                strategiesByType.Add(quoteStrategy.Type, quoteStrategy);

            _quoteStrategies = strategiesByType;
        }

        public QuoteDetail GetQuoteDetail()
        {
            var quoteDetail = new QuoteDetail();

            quoteDetail.Makes.Add("Ford");
            quoteDetail.Makes.Add("Audi");
            quoteDetail.Makes.Add("BMW");

            var modelSpec = new ModelSpec { Make = "Ford" };
            modelSpec.Models.AddRange(new []{ "Fiesta", "Focus", "Puma", "S Max" });
            quoteDetail.Models.Add(modelSpec);

            modelSpec = new ModelSpec { Make = "Audi" };
            modelSpec.Models.AddRange(new[] { "A3", "A4", "A5" });
            quoteDetail.Models.Add(modelSpec);

            modelSpec = new ModelSpec { Make = "BMW" };
            modelSpec.Models.AddRange(new[] { "X5", "3 Series", "5 Series" });
            quoteDetail.Models.Add(modelSpec);

            return quoteDetail;
        }

        public QuoteCalculationResult PerformQuote(QuoteRequest request)
        {
            var ageEligibilityError = GetAgeEligibilityError(request.DateOfBirth);
            if (ageEligibilityError != null)
                return QuoteCalculationResult.Failure(ageEligibilityError);

            if (!request.InsuranceType.HasValue)
                return QuoteCalculationResult.Failure("An insurance type is required to calculate a quote.");

            IQuoteStrategy quoteStrategy;
            var strategyFound = _quoteStrategies.TryGetValue(request.InsuranceType.Value, out quoteStrategy);
            if (!strategyFound)
                return QuoteCalculationResult.Failure("A quote is not available for the selected insurance type.");

            return QuoteCalculationResult.Success(quoteStrategy.Calculate(request));
        }

        private static string GetAgeEligibilityError(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue)
                return "Date of birth is required to calculate a quote.";

            var birthDate = dateOfBirth.Value.Date;
            var today = DateTime.Today;
            if (birthDate > today)
                return "Date of birth cannot be in the future.";

            var age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age))
                age--;

            if (age < 17)
                return "You must be at least 17 years old to receive a quote.";
            if (age > 80)
                return "You must be 80 years old or younger to receive a quote.";

            return null;
        }
    }
}
