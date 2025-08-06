using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using DevExpress.AIIntegration.Services.Assistant;
using DevExpress.Utils;

namespace DashboardAIAssistant.Services {
    public class AIAssistantProvider : IAIAssistantProvider, IAsyncDisposable {
        private readonly IAIAssistantFactory assistantFactory;
        private readonly AIAssistantManager assistantManager;
        private ConcurrentDictionary<string, (IAIAssistant, AIAssistantData)> Assistants { get; set; } = new();

        public AIAssistantProvider(IAIAssistantFactory assistantFactory, AIAssistantManager assistantManager) {
            this.assistantFactory = assistantFactory;
            this.assistantManager = assistantManager;
        }

        public async Task<string> CreateAssistant(Stream fileContent, string prompt) {
            Guard.ArgumentNotNull(fileContent, nameof(fileContent));
            Guard.ArgumentIsNotNullOrEmpty(prompt, nameof(prompt));

            string assistantName = Guid.NewGuid().ToString();
            var assistantData = await assistantManager.CreateAssistantAndThreadAsync(fileContent, $"{assistantName}.xlsx", prompt);

            IAIAssistant assistant = await assistantFactory.GetAssistant(assistantData.AssistantId, assistantData.ThreadId);
            await assistant.InitializeAsync();

            Assistants.TryAdd(assistantName, (assistant, assistantData));

            return assistantName;
        }

        public IAIAssistant GetAssistant(string assistantName) {
            Guard.ArgumentIsNotNullOrEmpty(assistantName, nameof(assistantName));

            if(!Assistants.TryGetValue(assistantName, out var tuple)) {
                throw new ArgumentException($"Incorrect assistant id: {assistantName}");
            }

            return tuple.Item1;
        }

        public async Task DisposeAssistant(string assistantName) {
            Guard.ArgumentIsNotNullOrEmpty(assistantName, nameof(assistantName));

            if(Assistants.TryRemove(assistantName, out var tuple)) {
                var (assistant, assistantData) = tuple;
                assistant.Dispose();
                await assistantManager.CleanUpAssistantAsync(assistantData);
            }
        }
        
        public async ValueTask DisposeAsync() {
            foreach(var (assistant, assistantData) in Assistants.Values) {
                assistant.Dispose();
                await assistantManager.CleanUpAssistantAsync(assistantData);
            }
            Assistants.Clear();
        }
    }
}

