using DevExpress.AIIntegration.Chat;
using System.IO;
using System.Threading.Tasks;

namespace DashboardAIAssistant.Services {
    public interface IAIDashboardChatService {
        IChatResponseProvider GetChatProvider(string sessionId);
        Task<string> OpenChatAsync(Stream excelStream);
        Task CloseChatAsync(string sessionId);
    }
}
