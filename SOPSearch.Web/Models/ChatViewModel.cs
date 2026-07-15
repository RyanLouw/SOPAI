using System.ComponentModel.DataAnnotations;

namespace SOPSearch.Web.Models
{
    public class ChatViewModel
    {
        [Required]
        [Display(Name = "Question")]
        public string Question { get; set; } = "";

        public string? SelectedTagSource { get; set; }

        public List<string> TagDataSource { get; set; } = new();

        public string? Answer { get; set; }
        public string? Error { get; set; }
    }
}
