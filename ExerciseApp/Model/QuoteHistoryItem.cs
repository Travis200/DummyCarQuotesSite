using System;

namespace ExerciseApp.Model
{
    public class QuoteHistoryItem
    {
        public Guid QuoteId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public InsuranceType InsuranceType { get; set; }
        public decimal Premium { get; set; }
    }
}
