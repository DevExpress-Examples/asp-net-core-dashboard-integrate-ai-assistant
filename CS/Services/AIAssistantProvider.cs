using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using DevExpress.AIIntegration.Services.Assistant;
using DevExpress.Utils;

namespace DashboardAIAssistant.Services {
    public class AIAssistantProvider : IAIAssistantProvider, IDisposable {
        private readonly IAIAssistantFactory assistantFactory;
        private readonly AIAssistantCreator assistantCreator;
        private ConcurrentDictionary<string, IAIAssistant> Assistants { get; set; } = new();

        public AIAssistantProvider(IAIAssistantFactory assistantFactory, AIAssistantCreator assistantCreator) {
            this.assistantFactory = assistantFactory;
            this.assistantCreator = assistantCreator;
        }

        public async Task<string> CreateAssistant(Stream fileContent, string prompt) {
            Guard.ArgumentNotNull(fileContent, nameof(fileContent));
            Guard.ArgumentIsNotNullOrEmpty(prompt, nameof(prompt));

            string guid = Guid.NewGuid().ToString();
            (string assistantId, string threadId) = await assistantCreator.CreateAssistantAndThreadAsync(fileContent, $"{guid}.xlsx", prompt);

            IAIAssistant assistant = await assistantFactory.GetAssistant(assistantId, threadId);
            await assistant.InitializeAsync();

            Assistants.TryAdd(assistantId, assistant);

            return assistantId;
        }

        public IAIAssistant GetAssistant(string assistantId) {
            Guard.ArgumentIsNotNullOrEmpty(assistantId, nameof(assistantId));

            IAIAssistant assistant = null;

            if(!Assistants.TryGetValue(assistantId, out assistant)) {
                throw new ArgumentException($"Incorrect assistant id: {assistantId}");
            }

            return assistant;
        }

        public async Task DisposeAssistant(string assistantId) {
            Guard.ArgumentIsNotNullOrEmpty(assistantId, nameof(assistantId));

            if(Assistants.TryRemove(assistantId, out IAIAssistant assistant)) {
                assistant.Dispose();
                await assistantCreator.CleanUpAssistantAsync(assistantId);
            }
        }
        
        public void Dispose() {
            foreach(var assistant in Assistants.Values) {
                assistant.Dispose();
            }
            Assistants.Clear();
        }
    }
}

