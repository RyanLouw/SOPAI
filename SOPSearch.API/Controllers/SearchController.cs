using Microsoft.AspNetCore.Mvc;
using SOPSearch.API.Services;
using SOPSearch.Models.Models;

namespace SOPSearch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly SearchIndexService _search;

        public SearchController(SearchIndexService search)
        {
            _search = search;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var docs = await _search.GetAll(ct);
            return Ok(docs);
        }

        [HttpGet("GetAllTags")]
        public async Task<IActionResult> GetAllTags(CancellationToken ct)
        {
            var tags = await _search.GetAllTags(ct);
            return Ok(tags);
        }

        [HttpPost("Delete")]
        public async Task<IActionResult> Delete([FromBody] string sopName, CancellationToken ct)
        {
            List<DocumentIndex> existing = (await _search.GetExistingMatches(new DocumentRequest() { SOPName = sopName }, ct))
                .Where(x => x.SOPName == sopName).ToList();
            await _search.Delete(existing, ct);
            return Ok();
        }

        //[HttpGet("CleanIndex")]
        //public async Task<IActionResult> CleanIndex()
        //{
        //    await _search.CleanIndex();
        //    return Ok();
        //}
    }
}
