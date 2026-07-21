using System.ComponentModel.DataAnnotations;

namespace SOPSearch.Web.Models
{
    public class SelectIndexViewModel
    {
        [Required(ErrorMessage = "Please select an index to continue.")]
        [Display(Name = "Index")]
        public string? SelectedTagSource { get; set; }

        public List<string> Indexes { get; set; } = new();
    }
}
