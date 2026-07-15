using SOPSearch.Models.Models;

namespace SOPSearch.Web.Models
{
    public class DocumentViewModel
    {
        public List<DocumentIndex>? Documents { get; set; }

        public string? Error { get; set; }
    }
}
