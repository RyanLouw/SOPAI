using SOPSearch.Models.Models;

namespace SOPSearch.Web.Models
{
    public class UploadViewModel : DocumentRequest
    {
        public string? Result { get; set; }
        public string? Error { get; set; }
    }
}
