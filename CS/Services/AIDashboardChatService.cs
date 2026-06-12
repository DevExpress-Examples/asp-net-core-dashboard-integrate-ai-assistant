using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using DevExpress.AIIntegration.Chat;

namespace DashboardAIAssistant.Services {
    public class AIDashboardChatService : IAIDashboardChatService, IAsyncDisposable {
        const string SESSION_NOT_FOUND_ERROR = "Chat session not found";

        readonly AgentFactory agentFactory;

        // Each chat session holds an IChatResponseProvider and a cleanup delegate that
        // removes the uploaded OpenAI resources (file + vector store) when the session ends.
        ConcurrentDictionary<string, (IChatResponseProvider Provider, Func<Task> Cleanup)> sessions = new();

        public AIDashboardChatService(AgentFactory agentFactory) {
            this.agentFactory = agentFactory;
        }

        async Task<string> RegisterSession(IChatResponseProvider provider, Func<Task> cleanup) {
            string sessionId = Guid.NewGuid().ToString();
            sessions.TryAdd(sessionId, (provider, cleanup));
            return sessionId;
        }

        // Opens an analytics chat for the current dashboard state.
        // The agent analyzes the exported Excel data and answers data-driven questions.
        public async Task<string> OpenDashboardChatAsync(Stream data) {
            var (provider, cleanup) = await agentFactory.CreateAgentWithFileAsync(
                data, Guid.NewGuid().ToString() + ".xlsx", AgentHelper.Prompt);
            return await RegisterSession(provider, cleanup);
        }

        public IChatResponseProvider GetChatProvider(string sessionId) {
            if(!string.IsNullOrEmpty(sessionId) && sessions.TryGetValue(sessionId, out var tuple))
                return tuple.Provider;
            throw new Exception(SESSION_NOT_FOUND_ERROR);
        }

        public async Task CloseChatAsync(string sessionId) {
            if(sessions.TryRemove(sessionId, out var tuple))
                await tuple.Cleanup();
            else
                throw new Exception(SESSION_NOT_FOUND_ERROR);
        }

        public async ValueTask DisposeAsync() {
            foreach(var (_, (_, cleanup)) in sessions)
                await cleanup();
            sessions.Clear();
        }
    }
}

