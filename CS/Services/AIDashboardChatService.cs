using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using DevExpress.AIIntegration.Chat;

namespace DashboardAIAssistant.Services {
    public class AIDashboardChatService : IAIDashboardChatService, IAsyncDisposable {
        const string SessionNotFoundError = "Chat session not found";

        readonly AgentFactory agentFactory;

        // Each chat session holds an IChatResponseProvider and a cleanup delegate that
        // removes the uploaded file when the session ends.
        ConcurrentDictionary<string, (IChatResponseProvider Provider, Func<Task> Cleanup)> sessions = new();

        public AIDashboardChatService(AgentFactory agentFactory) {
            this.agentFactory = agentFactory;
        }

        async Task<string> RegisterSession(IChatResponseProvider provider, Func<Task> cleanup) {
            string sessionId = Guid.NewGuid().ToString();
            sessions.TryAdd(sessionId, (provider, cleanup));
            return sessionId;
        }

        // Open an analytics chat for the current dashboard state.
        // The agent analyzes the exported Excel data and answers data-driven questions.
        public async Task<string> OpenChatAsync(Stream excelStream) {
            var (provider, cleanup) = await agentFactory.CreateChatProviderAsync(
                excelStream, Guid.NewGuid().ToString() + ".xlsx", AgentInstructions.Prompt);
            return await RegisterSession(provider, cleanup);
        }

        public IChatResponseProvider GetChatProvider(string sessionId) {
            if(!string.IsNullOrEmpty(sessionId) && sessions.TryGetValue(sessionId, out var tuple))
                return tuple.Provider;
            throw new Exception(SessionNotFoundError);
        }

        public async Task CloseChatAsync(string sessionId) {
            if(sessions.TryRemove(sessionId, out var tuple))
                await tuple.Cleanup();
            else
                throw new Exception(SessionNotFoundError);
        }

        public async ValueTask DisposeAsync() {
            foreach(var (_, (_, cleanup)) in sessions)
                await cleanup();
            sessions.Clear();
        }
    }
}

