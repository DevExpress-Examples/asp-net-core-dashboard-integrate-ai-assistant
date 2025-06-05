using DevExpress.AIIntegration.Services.Assistant;
using System.IO;
using System.Threading.Tasks;

namespace DashboardAIAssistant.Services {
    public interface IAIAssistantProvider {
        Task<string> CreateAssistant(Stream fileContent, string prompt);
        IAIAssistant GetAssistant(string assistantName);
        void DisposeAssistant(string assistantName);
    }
}
