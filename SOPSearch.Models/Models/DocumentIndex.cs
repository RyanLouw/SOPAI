using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SOPSearch.Models.Models
{
    public record DocumentIndex
    {
        public Guid ID { get; set; }

        [Display(Name = "SOP Name")]
        public string SOPName { get; set; } = "";

        [Display(Name = "Doc Lib Location")]
        public string DocLibLocation { get; set; } = "";
        public string Content { get; set; } = "";
        public float[] Embedding { get; set; } = [];
        public List<string> Tags { get; set; } = new();
        public string? Key { get; set; }

        [Display(Name = "Pieces")]
        public int? IndexCount { get; set; }

        public string GetQueryString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"SOP Name: {SOPName}");
            sb.AppendLine($"Document Location: {DocLibLocation}");
            sb.AppendLine($"Document Content: {Content}");
            return sb.ToString();
        }
    }
}
