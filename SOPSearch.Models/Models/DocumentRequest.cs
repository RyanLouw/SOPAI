using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SOPSearch.Models.Models
{
    public class DocumentRequest
    {
        [Required]
        [Display(Name = "File")]
        public IFormFile? File { get; set; }

        [Required]
        [Display(Name = "SOP Name")]
        public string SOPName { get; set; } = "";

        [Required]
        [Display(Name = "Doc Lib Location")]
        public string DocLibLocation { get; set; } = "";

        public List<string> Tags { get; set; } = new();
    }
}
