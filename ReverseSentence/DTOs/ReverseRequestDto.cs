using System.ComponentModel.DataAnnotations;

namespace ReverseSentence.DTOs
{
    public class ReverseRequestDto
    {
        [Required(ErrorMessage = "Sentence is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Sentence must be between 1 and 1000 characters")]
        public string Sentence { get; set; } = string.Empty;
    }
}
