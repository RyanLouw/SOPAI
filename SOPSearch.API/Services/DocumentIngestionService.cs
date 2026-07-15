using Azure;
using Azure.AI.DocumentIntelligence;

namespace SOPSearch.API.Services
{
    public class DocumentIngestionService
    {
        private readonly DocumentIntelligenceClient _client;

        public DocumentIngestionService(IConfiguration config)
        {
            _client = new DocumentIntelligenceClient(
                new Uri(config["Azure:DocumentIntelligence:Endpoint"] ?? ""),
                new AzureKeyCredential(config["Azure:DocumentIntelligence:Key"] ?? "")
            );
        }

        public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken ct)
        {
            fileStream.Position = 0;
            BinaryData data = BinaryData.FromStream(fileStream);

            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                new AnalyzeDocumentOptions("prebuilt-layout", data),
                ct
            );

            return string.Join("\n", operation.Value.Paragraphs.Where(x => x.Role == null || (x.Role != ParagraphRole.PageFooter && x.Role != ParagraphRole.SectionHeading)).Select(p => p.Content));
        }
    }
}
