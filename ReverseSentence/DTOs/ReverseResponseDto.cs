namespace ReverseSentence.DTOs
{
    public class ReverseResponseDto
    {
        public string OriginalSentence { get; set; } = string.Empty;
        public string ReversedSentence { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
