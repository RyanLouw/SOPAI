using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.VisualBasic.FileIO;
using SOPSearch.Models.Helper;
using SOPSearch.Models.Models;
using System.Collections;

namespace SOPSearch.API.Services
{
    public class SearchIndexService
    {
        private readonly SearchClient _searchClient;
        private readonly IConfiguration _configuration;

        public SearchIndexService(IConfiguration config)
        {
            _configuration = config;
            _searchClient = new SearchClient(
                new Uri(config["Azure:Search:Endpoint"] ?? ""),
                config["Azure:Search:IndexName"],
                new AzureKeyCredential(config["Azure:Search:Key"] ?? "")
            );
        }

        public async Task<string?> IndexChunkAsync(DocumentIndex index, CancellationToken ct)
        {
            var doc = new
            {
                id = index.ID,
                content = index.Content,
                embedding = index.Embedding,
                docLibLocation = index.DocLibLocation,
                sopName = index.SOPName,
                tag = index.Tags
            };

            var response = await _searchClient.UploadDocumentsAsync(new[] { doc }, cancellationToken: ct);
            return response.Value.Results.FirstOrDefault()?.Key;
        }

        public async Task<List<DocumentIndex>> SearchAsync(float[] embedding, string? tag, CancellationToken ct)
        {
            var options = new SearchOptions
            {
                SearchFields = { "tag" },
                Select = { "sopName", "docLibLocation", "content" },
                VectorSearch = new()
                {
                    Queries =
                    {
                        new VectorizedQuery(embedding)
                        {
                            KNearestNeighborsCount = 5,
                            Fields = { "embedding" }
                        }
                    }
                },
                Size = 5,
                QueryType = SearchQueryType.Semantic,
                SemanticSearch = new() { SemanticConfigurationName = _configuration["Azure:Search:SemanticConfig"] }
            };

            var response = await _searchClient.SearchAsync<SearchDocument>(tag ?? "*", options, cancellationToken: ct);

            List<DocumentIndex> documents = new List<DocumentIndex>();

            await foreach (SearchResult<SearchDocument> result in response.Value.GetResultsAsync())
            {
                SearchDocument doc = result.Document;
                documents.Add(new DocumentIndex()
                {
                    SOPName = (string)doc["sopName"],
                    DocLibLocation = (string)doc["docLibLocation"],
                    Content = (string)doc["content"]
                });
            }

            return documents;
        }

        public async Task<List<DocumentIndex>> GetExistingMatches(DocumentRequest request, CancellationToken ct)
        {
            var options = new SearchOptions() { QueryType = SearchQueryType.Full };
            options.SearchFields.Add("sopName");
            options.Select.Add("id");
            options.Select.Add("sopName");
            options.Select.Add("docLibLocation");
            options.Select.Add("content");

            var response = await _searchClient.SearchAsync<SearchDocument>($"{request.SOPName}", options, cancellationToken: ct);

            List<DocumentIndex> documents = new List<DocumentIndex>();

            await foreach (SearchResult<SearchDocument> result in response.Value.GetResultsAsync())
            {
                SearchDocument doc = result.Document;
                documents.Add(new DocumentIndex()
                {
                    Key = (string)doc["id"],
                    SOPName = (string)doc["sopName"],
                    DocLibLocation = (string)doc["docLibLocation"]
                });
            }

            return documents;
        }

        public async Task<List<DocumentIndex>> GetAll(CancellationToken ct)
        {
            var response = await _searchClient.SearchAsync<SearchDocument>("*", cancellationToken: ct);
            List<DocumentIndex> documents = new List<DocumentIndex>();

            await foreach (SearchResult<SearchDocument> result in response.Value.GetResultsAsync())
            {
                SearchDocument doc = result.Document;
                string sopName = (string)doc["sopName"];

                List<string> Tags = ((IEnumerable)doc["tag"]).Cast<object>()
                        .OfType<string>()
                        .ToList();

                if (!documents.Any(x => x.SOPName == sopName))
                {
                    documents.Add(new DocumentIndex()
                    {
                        Key = (string)doc["id"],
                        SOPName = sopName,
                        DocLibLocation = (string)doc["docLibLocation"],
                        Tags = Tags,
                        IndexCount = 1
                    });
                }
                else
                    documents.Where(x => x.SOPName == sopName)
                        .Select(x => { x.IndexCount++; return x; }).ToList();
            }

            documents.Sort((x, y) => new NaturalStringComparer().Compare(x.SOPName, y.SOPName));

            return documents;
        }

        public async Task<List<string>> GetAllTags(CancellationToken ct)
        {
            var options = new SearchOptions
            {
                Select = { "tag" },
                //Filter = "not match(tag, '^[0-9].*')"
            };

            var results = await _searchClient.SearchAsync<SearchDocument>("*", options);

            var distinctTags = results.Value.GetResults()
                .SelectMany(r => ((IEnumerable)r.Document["tag"]).Cast<object>().OfType<string>().ToList())
                .Where(t => !string.IsNullOrWhiteSpace(t) && !char.IsDigit(t[0]))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return distinctTags;
        }

        public async Task Delete(List<DocumentIndex> documents, CancellationToken ct)
        {
            if (documents.Count > 0)
                await _searchClient.DeleteDocumentsAsync("id", documents.Select(x => x.Key).ToList(), cancellationToken: ct);
        }

        public async Task CleanIndex(CancellationToken ct)
        {
            var response = await _searchClient.SearchAsync<SearchDocument>("*", new SearchOptions() { Size = 5 }, cancellationToken: ct);
            var results = response.Value.GetResultsAsync();

            if (await results.AnyAsync())
            {
                do
                {
                    await _searchClient.DeleteDocumentsAsync("id", await results.Select(x => (string)x.Document["id"]).ToListAsync(), cancellationToken: ct);

                    response = await _searchClient.SearchAsync<SearchDocument>("*", new SearchOptions() { Size = 5 }, cancellationToken: ct);
                    results = response.Value.GetResultsAsync();
                } while (false);
            }
        }
    }
}
