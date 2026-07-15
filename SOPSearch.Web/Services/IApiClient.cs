using SOPSearch.Models.Models;

namespace SOPSearch.Web.Services
{
    public interface IApiClient
    {
        Task<List<string>?> GetAllTags(CancellationToken ct = default);
        Task<string> AskChatAsync(string question, string? tag, CancellationToken ct = default);
        Task<string> UploadFileAsync(DocumentRequest request, CancellationToken ct = default);
        Task<List<DocumentIndex>?> GetAllDocuments(CancellationToken ct = default);
        Task<string> DeleteDocument(string sopName, CancellationToken ct = default);

    }
}
