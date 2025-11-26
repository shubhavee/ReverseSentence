using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReverseSentence.DTOs;
using ReverseSentence.Services;

namespace ReverseSentence.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReverseController : ControllerBase
    {
        private readonly IReverseService reverseService;
        private readonly ILogger<ReverseController> logger;

        public ReverseController(IReverseService reverseService, ILogger<ReverseController> logger)
        {
            this.reverseService = reverseService;
            this.logger = logger;
        }

        /// <summary>
        /// Reverses all words in the provided sentence
        /// </summary>
        /// <param name="request">The sentence to reverse</param>
        /// <returns>The reversed sentence along with the original</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ReverseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReverseResponseDto>> ReverseSentence(
            [FromBody] ReverseRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await reverseService.ReverseWordsAsync(request.Sentence);
            return Ok(result);
        }

        /// <summary>
        /// Search for request/response pairs by word
        /// </summary>
        /// <param name="word">The word to search for</param>
        /// <returns>List of matching request/response pairs</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<HistoryItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<HistoryItemDto>>> SearchByWord(
            [FromQuery] string word,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return BadRequest(new { error = "Search word cannot be empty" });
            }

            var results = await reverseService.SearchByWordAsync(word);
            return Ok(results);
        }

        /// <summary>
        /// Get history of all request/response pairs
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
        /// <returns>Paginated list of request/response pairs</returns>
        [HttpGet("history")]
        [ProducesResponseType(typeof(PagedResponse<HistoryItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<HistoryItemDto>>> GetHistory(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
            {
                return BadRequest(new { error = "Page must be greater than 0" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "PageSize must be between 1 and 100" });
            }

            var result = await reverseService.GetHistoryPagedAsync(page, pageSize);
            return Ok(result);
        }
    }
}
