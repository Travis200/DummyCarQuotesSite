using ExerciseApp.Model;
using ExerciseApp.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExerciseApp.Service
{
    public class QuoteService
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> VehicleModels =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ford"] = new[] { "Fiesta", "Focus", "Puma", "S Max" },
                ["Audi"] = new[] { "A3", "A4", "A5" },
                ["BMW"] = new[] { "X5", "3 Series", "5 Series" }
            };

        private readonly IReadOnlyDictionary<InsuranceType, IQuoteStrategy> _quoteStrategies;
        private readonly IQuoteRepository _quoteRepository;

        public QuoteService(IEnumerable<IQuoteStrategy> quoteStrategies, IQuoteRepository quoteRepository)
        {
            var strategiesByType = new Dictionary<InsuranceType, IQuoteStrategy>();
            foreach (var quoteStrategy in quoteStrategies)
                strategiesByType.Add(quoteStrategy.Type, quoteStrategy);

            _quoteStrategies = strategiesByType;
            _quoteRepository = quoteRepository;
        }

        public QuoteDetail GetQuoteDetail()
        {
            var quoteDetail = new QuoteDetail();

            foreach (var make in VehicleModels)
            {
                quoteDetail.Makes.Add(make.Key);
                var modelSpec = new ModelSpec { Make = make.Key };
                modelSpec.Models.AddRange(make.Value);
                quoteDetail.Models.Add(modelSpec);
            }

            return quoteDetail;
        }

        public async Task<QuoteCalculationResult> PerformQuoteAsync(
            QuoteRequest request,
            CancellationToken cancellationToken = default)
        {
            string normalizedMake;
            string normalizedModel;
            var vehicleFound = TryResolveVehicle(
                request.Make,
                request.Model,
                out normalizedMake,
                out normalizedModel);
            if (!vehicleFound)
                return QuoteCalculationResult.Failure(
                    $"A quote is unavailable for make '{request.Make}' and model '{request.Model}'.");

            var ageEligibilityError = GetAgeEligibilityError(request.DateOfBirth);
            if (ageEligibilityError != null)
                return QuoteCalculationResult.Failure(ageEligibilityError);

            if (!request.InsuranceType.HasValue)
                return QuoteCalculationResult.Failure("An insurance type is required to calculate a quote.");

            IQuoteStrategy quoteStrategy;
            var strategyFound = _quoteStrategies.TryGetValue(request.InsuranceType.Value, out quoteStrategy);
            if (!strategyFound)
                return QuoteCalculationResult.Failure("A quote is not available for the selected insurance type.");

            var normalizedRequest = new QuoteRequest
            {
                DateOfBirth = request.DateOfBirth,
                InsuranceType = request.InsuranceType,
                Make = normalizedMake,
                Model = normalizedModel
            };
            var premium = quoteStrategy.Calculate(normalizedRequest);
            var savedQuote = new QuoteRecord
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                DateOfBirth = request.DateOfBirth.Value.Date,
                Make = normalizedMake,
                Model = normalizedModel,
                InsuranceType = request.InsuranceType.Value,
                Premium = premium
            };

            await _quoteRepository.AddAsync(savedQuote, cancellationToken);
            return QuoteCalculationResult.Success(savedQuote.Id, premium);
        }

        public Task<QuoteRecord> GetQuoteByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _quoteRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<IReadOnlyList<QuoteHistoryItem>> GetPreviousQuotesAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            var quotes = await _quoteRepository.GetLatestAsync(count, cancellationToken);
            var history = new List<QuoteHistoryItem>(quotes.Count);

            foreach (var quote in quotes)
            {
                history.Add(new QuoteHistoryItem
                {
                    QuoteId = quote.Id,
                    CreatedAtUtc = quote.CreatedAtUtc,
                    Make = quote.Make,
                    Model = quote.Model,
                    InsuranceType = quote.InsuranceType,
                    Premium = quote.Premium
                });
            }

            return history;
        }

        private static bool TryResolveVehicle(
            string userEnteredMake,
            string userEnteredModel,
            out string normalizedMake,
            out string normalizedModel)
        {
            normalizedMake = null;
            normalizedModel = null;
            if (string.IsNullOrWhiteSpace(userEnteredMake) || string.IsNullOrWhiteSpace(userEnteredModel))
                return false;

            foreach (var supportedMake in VehicleModels)
            {
                if (!string.Equals(supportedMake.Key, userEnteredMake.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var supportedModel in supportedMake.Value)
                {
                    if (!string.Equals(supportedModel, userEnteredModel.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    normalizedMake = supportedMake.Key;
                    normalizedModel = supportedModel;
                    return true;
                }

                return false;
            }

            return false;
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
