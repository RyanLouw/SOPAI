namespace SOPSearch.Models.Models
{
    public record ChatRequest
    {
        public string Question { get; set; } = "";
        public string? Tag { get; set; }
        public ChatFormat Format { get; set; }
    }
}
