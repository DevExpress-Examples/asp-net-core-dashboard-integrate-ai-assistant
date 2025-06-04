using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using DevExpress.AIIntegration.Services.Assistant;
using DevExpress.Utils;

namespace DashboardAIAssistant.Services {
    public class AIAssistantProvider : IAIAssistantProvider {
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

            string assistantName = Guid.NewGuid().ToString();
            (string assistantId, string threadId) = await assistantCreator.CreateAssistantAndThreadAsync(fileContent, $"{assistantName}.xlsx", prompt);

            IAIAssistant assistant = await assistantFactory.GetAssistant(assistantId, threadId);
            await assistant.InitializeAsync();

            Assistants.TryAdd(assistantName, assistant);

            return assistantName;
        }

        public IAIAssistant GetAssistant(string assistantName) {
            Guard.ArgumentIsNotNullOrEmpty(assistantName, nameof(assistantName));

            IAIAssistant assistant = null;

            if(!Assistants.TryGetValue(assistantName, out assistant)) {
                throw new ArgumentException($"Incorrect assistant id: {assistantName}");
            }

            return assistant;
        }

        public void DisposeAssistant(string assistantName) {
            Guard.ArgumentIsNotNullOrEmpty(assistantName, nameof(assistantName));

            if(Assistants.TryRemove(assistantName, out IAIAssistant assistant)) {
                assistant.Dispose();
            }
        }
    }
}

