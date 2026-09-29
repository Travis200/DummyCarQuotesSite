using System;

namespace ExerciseApp.Model
{
    public class QuoteResponse
    {
       public bool RequestValid { get; set; }
       public bool QuoteAvailable { get; set; }
       public Guid? QuoteId { get; set; }
       public decimal? Quote { get; set; }
       public string ErrorMessage { get; set; }
    }
}
