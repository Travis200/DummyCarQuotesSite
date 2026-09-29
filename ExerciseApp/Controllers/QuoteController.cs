using ExerciseApp.Model;
using ExerciseApp.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace ExerciseApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuoteController : ControllerBase
    {
        private readonly QuoteService _quoteService;
        private readonly ILogger<QuoteController> _logger;

        public QuoteController(QuoteService quoteService, ILogger<QuoteController> logger)
        {
            _quoteService = quoteService;
            _logger = logger;
        }

        [HttpGet]
        public QuoteDetail Get()
        {
            return _quoteService.GetQuoteDetail();
        }

        [HttpPost]
        public ActionResult<QuoteResponse> Post(QuoteRequest request)
        {
            var response = new QuoteResponse { RequestValid = false, QuoteAvailable = false };
            if (!TryValidateModel(request))
            {
                response.ErrorMessage = string.Join("; ", ModelState.Values
                    .SelectMany(entry => entry.Errors)
                    .Select(error => error.ErrorMessage));
                return response;
            }

            response.RequestValid = true;
            try
            {
                var quoteResult = _quoteService.PerformQuote(request);
                response.QuoteAvailable = quoteResult.IsSuccess;
                response.Quote = quoteResult.Quote;
                response.ErrorMessage = quoteResult.ErrorMessage;
                return response;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An unexpected error occurred while calculating a quote.");
                response.ErrorMessage = "An unexpected error occurred while calculating the quote. Please try again later.";
                return StatusCode(500, response);
            }
        }

    }
}
