using Azure;
using Azure.AI.OpenAI;
using Microsoft.ML.Tokenizers;
using OpenAI.Chat;
using SOPSearch.Models.Models;

namespace SOPSearch.API.Services
{
    public class OpenAiService
    {
        private readonly AzureOpenAIClient _embedClient;
        private readonly AzureOpenAIClient _chatClient;
        private static string systemMessage = "You are a bot designed to give summaries for SOP's and references to the documents." +
            " Answer using only the context provided. Provide references to the SOP name and location. Explain your purpose if asked to do so." +
            " When working with multiple SOP's give structured response per SOP as each SOP does not relate to the next one.";

        private readonly IConfiguration _config;

        public OpenAiService(IConfiguration config)
        {
            _config = config;
            _embedClient = new AzureOpenAIClient(
                new Uri(config["Azure:OpenAIEmbed:Endpoint"] ?? ""),
                new AzureKeyCredential(config["Azure:OpenAIEmbed:Key"] ?? "")
            );
            _chatClient = new AzureOpenAIClient(
                new Uri(config["Azure:OpenAIEmbed:Endpoint"] ?? ""),
                new AzureKeyCredential(config["Azure:OpenAIEmbed:Key"] ?? "")
            );
        }

        public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct)
        {
            var client = _embedClient.GetEmbeddingClient(
                _config["Azure:OpenAIEmbed:ChatDeployment"] ?? ""
            );
            var result = await client.GenerateEmbeddingAsync(text, new OpenAI.Embeddings.EmbeddingGenerationOptions { Dimensions = int.TryParse(_config["Azure:OpenAIEmbed:EmbeddingSize"], out int size) ? size : 512 }, ct);
            return result.Value.ToFloats().ToArray();
        }

        public async Task<string> ChatAsync(string prompt, ChatFormat format, CancellationToken ct)
        {
            List<ChatMessage> messages = new List<ChatMessage>()
            {
                new SystemChatMessage(GetSystemMessage(format)),
                new UserChatMessage(prompt)
            };

            int? tokenCount = null;

            try
            {
                var tokenizer = TiktokenTokenizer.CreateForModel(_config["Azure:OpenAIChat:ChatModel"] ?? "");
                tokenCount = tokenizer.CountTokens(systemMessage + prompt);
                tokenCount = tokenCount + (int.TryParse(_config["Azure:OpenAIChat:TokenLimit"], out int limit) ? limit : 5000);
            }
            catch { }

            var client = _chatClient.GetChatClient(
                _config["Azure:OpenAIChat:ChatDeployment"] ?? ""
            );

            var options = new ChatCompletionOptions() { MaxOutputTokenCount = tokenCount };

            switch ((_config["Azure:OpenAIChat:ChatModel"] ?? "").ToLower())
            {
                case "gpt-4.1":
                case "gpt-4o":
                    options.Temperature = 0f;
                    options.FrequencyPenalty = 0.1f;
                    break;
                case "gpt-5":
                case "gpt-5.4":
                    options.FrequencyPenalty = 0.1f;
                    break;
                default:
                    break;
            }

            var result = await client.CompleteChatAsync(messages, options, ct);
            return string.Join("\n", result?.Value.Content.Select(x => x.Text) ?? new List<string>());
        }

        private string GetSystemMessage(ChatFormat format)
        {
            switch (format)
            {
                case ChatFormat.HTML:
                    return $"{systemMessage} Format the output as HTML. Do not add the html tag at the top.";
                case ChatFormat.Markdown:
                    return $"{systemMessage} Format the output as markdown.";
            }
            return systemMessage;
        }
    }
}
