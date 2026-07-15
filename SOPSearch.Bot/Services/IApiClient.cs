using System.Threading;
using System.Threading.Tasks;

namespace SOPSearch.Bot.Services
{
    public interface IApiClient
    {
        Task<string> AskChatAsync(string question, CancellationToken ct = default);
    }
}
