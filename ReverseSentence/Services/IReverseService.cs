using ReverseSentence.DTOs;

namespace ReverseSentence.Services
{
    public interface IReverseService
    {
        Task<ReverseResponseDto> ReverseWordsAsync(string sentence);
        Task<PagedResponse<HistoryItemDto>> GetHistoryPagedAsync(int page, int pageSize);
        Task<IEnumerable<HistoryItemDto>> SearchByWordAsync(string word);
    }
}
