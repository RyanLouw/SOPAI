using SOPSearch.Models.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SOPSearch.Web.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<string>?> GetAllTags(CancellationToken ct = default)
        {
            using var resp = await _http.GetAsync("api/Search/GetAllTags", ct);
            var body = await resp.Content.ReadFromJsonAsync<List<string>>(ct);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"ERROR ({(int)resp.StatusCode}): {body}");

            return body;
        }

        public async Task<string> AskChatAsync(string question, string? tag, CancellationToken ct = default)
        {
            var payload = new ChatRequest { Question = question ?? "", Tag = tag, Format = ChatFormat.HTML };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync("api/Chat", content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return $"ERROR ({(int)resp.StatusCode}): {body}";

            return body;
        }

        public async Task<string> UploadFileAsync(DocumentRequest request, CancellationToken ct = default)
        {
            if (request.File == null || request.File.Length == 0)
                return "Please choose a file.";

            using var form = new MultipartFormDataContent();

            using var fileStream = request.File.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(request.File.ContentType);

            form.Add(fileContent, "File", request.File.FileName);

            form.Add(new StringContent(request.SOPName), "SOPName");
            form.Add(new StringContent(request.DocLibLocation), "DocLibLocation");

            if (request.Tags != null)
            {
                foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    form.Add(new StringContent(tag.Trim()), "Tags");
                }
            }

            using var resp = await _http.PostAsync("api/Upload", form, ct);

            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return $"ERROR ({(int)resp.StatusCode}): {body}";

            return body;
        }

        public async Task<List<DocumentIndex>?> GetAllDocuments(CancellationToken ct = default)
        {
            using var resp = await _http.GetAsync("api/Search/GetAll", ct);
            var body = await resp.Content.ReadFromJsonAsync<List<DocumentIndex>>(ct);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"ERROR ({(int)resp.StatusCode}): {body}");

            return body;
        }

        public async Task<string> DeleteDocument(string sopName, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(sopName, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync("api/Search/Delete", content, ct);

            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return $"ERROR ({(int)resp.StatusCode}): {body}";

            return body;
        }
    }
}
