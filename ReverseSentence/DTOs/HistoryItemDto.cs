namespace ReverseSentence.DTOs
{
    public class HistoryItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string OriginalSentence { get; set; } = string.Empty;
        public string ReversedSentence { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
