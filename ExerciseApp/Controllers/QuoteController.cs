using ExerciseApp.Model;
using ExerciseApp.Service;
using ExerciseApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        [HttpGet("~/Quotes")]
        public async Task<ActionResult<IReadOnlyList<QuoteHistoryItem>>> GetPreviousQuotes(
            [FromQuery] int count = 5,
            CancellationToken cancellationToken = default)
        {
            if (count < 1 || count > 100)
                return BadRequest(new { errorMessage = "Count must be between 1 and 100." });

            var quotes = await _quoteService.GetPreviousQuotesAsync(count, cancellationToken);
            return Ok(quotes);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<QuoteRecord>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var quote = await _quoteService.GetQuoteByIdAsync(id, cancellationToken);
            if (quote == null)
                return NotFound();

            return quote;
        }

        [HttpPost]
        public async Task<ActionResult<QuoteResponse>> Post(QuoteRequest request, CancellationToken cancellationToken)
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
                var quoteResult = await _quoteService.PerformQuoteAsync(request, cancellationToken);
                response.QuoteAvailable = quoteResult.IsSuccess;
                response.QuoteId = quoteResult.QuoteId;
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
