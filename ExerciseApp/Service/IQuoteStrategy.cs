using ExerciseApp.Model;

namespace ExerciseApp.Service
{
    public interface IQuoteStrategy
    {
        InsuranceType Type { get; }
        decimal Calculate(QuoteRequest request);
    }
}
