using Microsoft.AspNetCore.Mvc;
using SOPSearch.API.Services;
using SOPSearch.Models.Models;

namespace SOPSearch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly OpenAiService _openAi;
        private readonly SearchIndexService _search;

        public ChatController(OpenAiService openAi, SearchIndexService search)
        {
            _openAi = openAi;
            _search = search;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
        {
            var queryEmbedding = await _openAi.CreateEmbeddingAsync(request.Question, ct);
            var documents = await _search.SearchAsync(queryEmbedding, request.Tag, ct);

            var prompt = $"""
                Context:
                {string.Join("\n", documents.Select(x => x.GetQueryString()))}

                Question:
                {request.Question}
                """;

            var answer = await _openAi.ChatAsync(prompt, request.Format, ct);
            answer = answer.TrimStart('`').TrimEnd('`');
            return Ok(answer);
        }
    }
}
