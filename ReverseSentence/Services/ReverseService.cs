using ReverseSentence.DTOs;
using ReverseSentence.Models;
using ReverseSentence.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace ReverseSentence.Services
{
    public class ReverseService : IReverseService
    {
        private readonly IReverseRepository repository;
        private readonly ICache cache;
        private readonly ICurrentUserService currentUserService;
        private readonly ILogger<ReverseService> logger;

        public ReverseService(
            IReverseRepository repository, 
            ICache cache, 
            ICurrentUserService currentUserService,
            ILogger<ReverseService> logger)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReverseResponseDto> ReverseWordsAsync(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                throw new ArgumentException("Sentence cannot be empty", nameof(sentence));
            }

            var userId = currentUserService.GetUserId();
            
            // Check cache first with SHA256-hashed key for security
            var cacheKey = GenerateCacheKey(userId, sentence);
            var cachedResult = await cache.GetAsync<ReverseResponseDto>(cacheKey);

            if (cachedResult != null)
            {
                logger.LogInformation("Cache hit for user {UserId}, sentence: '{Sentence}'", userId, sentence);
                return cachedResult;
            }

            logger.LogInformation("Cache miss for user {UserId}, sentence: '{Sentence}'", userId, sentence);

            var reversedSentence = ReverseWords(sentence);

            var request = new ReverseRequest
            {
                UserId = userId,
                OriginalSentence = sentence,
                ReversedSentence = reversedSentence,
                CreatedAt = DateTime.UtcNow
            };

            await repository.CreateAsync(request);

            logger.LogInformation("User {UserId} reversed sentence: '{Original}' -> '{Reversed}'", userId, sentence, reversedSentence);

            var response = new ReverseResponseDto
            {
                OriginalSentence = sentence,
                ReversedSentence = reversedSentence,
                Timestamp = request.CreatedAt
            };

            // Cache the result with 24-hour sliding expiration
            await cache.SetAsync(cacheKey, response, TimeSpan.FromHours(24));

            return response;
        }

        public async Task<PagedResponse<HistoryItemDto>> GetHistoryPagedAsync(int page, int pageSize)
        {
            if (page < 1) throw new ArgumentException("Page must be greater than 0", nameof(page));
            if (pageSize < 1 || pageSize > 100) throw new ArgumentException("PageSize must be between 1 and 100", nameof(pageSize));

            var userId = currentUserService.GetUserId();
            var (items, totalCount) = await repository.GetPagedAsync(userId, page, pageSize);

            var data = items.Select(h => new HistoryItemDto
            {
                Id = h.Id ?? string.Empty,
                OriginalSentence = h.OriginalSentence,
                ReversedSentence = h.ReversedSentence,
                CreatedAt = h.CreatedAt
            });

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResponse<HistoryItemDto>
            {
                Data = data,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = (int)totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<IEnumerable<HistoryItemDto>> SearchByWordAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                throw new ArgumentException("Search word cannot be empty", nameof(word));
            }

            // Sanitize and validate input
            var sanitizedWord = word.Trim();
            if (sanitizedWord.Length > 100)
            {
                throw new ArgumentException("Search word too long (max 100 characters)", nameof(word));
            }

            var userId = currentUserService.GetUserId();
            var results = await repository.SearchByWordAsync(userId, sanitizedWord);

            return results.Select(r => new HistoryItemDto
            {
                Id = r.Id ?? string.Empty,
                OriginalSentence = r.OriginalSentence,
                ReversedSentence = r.ReversedSentence,
                CreatedAt = r.CreatedAt
            });
        }

        private static string ReverseWords(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                return sentence;
            }

            var words = sentence.Split(' ', StringSplitOptions.None);
            var reversedWords = words.Select(word => new string(word.Reverse().ToArray()));

            return string.Join(' ', reversedWords);
        }

        /// <summary>
        /// Generates a safe cache key using SHA256 hashing to prevent collision attacks
        /// </summary>
        private static string GenerateCacheKey(string userId, string sentence)
        {
            var input = $"{userId}|{sentence}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var hashString = Convert.ToHexString(hashBytes);
            return $"reverse:{hashString}";
        }
    }
}
