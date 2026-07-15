using SOPSearch.Models.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SOPSearch.Bot.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> AskChatAsync(string question, CancellationToken ct = default)
        {
            var payload = new ChatRequest { Question = question ?? "", Format = ChatFormat.Markdown };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync("api/Chat", content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return $"ERROR ({(int)resp.StatusCode}): {body}";

            return body;
        }
    }
}


// TODO add Emulator 