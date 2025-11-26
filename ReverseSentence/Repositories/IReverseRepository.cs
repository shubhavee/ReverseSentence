using ReverseSentence.Models;

namespace ReverseSentence.Repositories
{
    public interface IReverseRepository
    {
        Task<ReverseRequest> CreateAsync(ReverseRequest request);
        Task<(IEnumerable<ReverseRequest> Items, long TotalCount)> GetPagedAsync(string userId, int page, int pageSize);
        Task<IEnumerable<ReverseRequest>> SearchByWordAsync(string userId, string word);
    }
}
