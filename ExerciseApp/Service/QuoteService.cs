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

        public decimal PerformQuote(QuoteRequest request)
        {
            IQuoteStrategy quoteStrategy;
            var strategyFound = _quoteStrategies.TryGetValue(request.InsuranceType.Value, out quoteStrategy);

            if (!IsEligibleByAge(request) || !request.InsuranceType.HasValue || !strategyFound)
                return 0;

            return quoteStrategy.Calculate(request);
        }

        private static bool IsEligibleByAge(QuoteRequest request)
        {
            if (!request.DateOfBirth.HasValue)
                return false;

            var dateOfBirth = request.DateOfBirth.Value.Date;
            var today = DateTime.Today;
            if (dateOfBirth > today)
                return false;

            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age >= 17 && age <= 80;
        }
    }
}
