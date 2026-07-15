using Microsoft.AspNetCore.Mvc;
using SOPSearch.API.Services;
using SOPSearch.Models.Models;

namespace SOPSearch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly DocumentIngestionService _docs;
        private readonly OpenAiService _openAi;
        private readonly SearchIndexService _search;

        public UploadController(
            DocumentIngestionService docs,
            OpenAiService openAi,
            SearchIndexService search)
        {
            _docs = docs;
            _openAi = openAi;
            _search = search;
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] DocumentRequest model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var stream = model.File?.OpenReadStream();
            if (stream == null)
                return BadRequest(ModelState);

            var current = await _search.GetExistingMatches(model, ct);
            await _search.Delete(current.Where(x => x.SOPName == model.SOPName).ToList(), ct);

            var text = await _docs.ExtractTextAsync(stream, ct);

            foreach (var chunk in text.Chunk(1000))
            {
                DocumentIndex index = new DocumentIndex()
                {
                    ID = Guid.NewGuid(),
                    Content = new string(chunk),
                    SOPName = model.SOPName,
                    DocLibLocation = model.DocLibLocation,
                    Tags = model.Tags
                };

                index.Embedding = await _openAi.CreateEmbeddingAsync(index.Content, ct);
                index.Key = await _search.IndexChunkAsync(index, ct);
            }

            return Ok();
        }
    }
}
