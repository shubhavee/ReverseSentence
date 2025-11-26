namespace ReverseSentence.DTOs
{
    public class PaginationRequest
    {
        private int _page = 1;
        private int _pageSize = 20;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value switch
            {
                < 1 => 20,
                > 100 => 100, // Max 100 items per page
                _ => value
            };
        }
    }
}
