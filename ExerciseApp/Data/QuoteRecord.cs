using ExerciseApp.Model;
using System;

namespace ExerciseApp.Data
{
    public class QuoteRecord
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public InsuranceType InsuranceType { get; set; }
        public decimal Premium { get; set; }
    }
}
