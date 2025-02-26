using DevExpress.AIIntegration.OpenAI.Services;
using DevExpress.AIIntegration.Services.Assistant;
using DevExpress.Utils;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace DashboardAIAssistant.Services {
    public class AIAssistantProvider : IAIAssistantProvider {
        private readonly IAIAssistantFactory assistantFactory;
        private ConcurrentDictionary<string, IAIAssistant> Assistants { get; set; } = new();

        public AIAssistantProvider(IAIAssistantFactory assistantFactory) {
            this.assistantFactory = assistantFactory;
        }

        public async Task<string> CreateAssistant(Stream fileContent, string prompt) {
            Guard.ArgumentNotNull(fileContent, nameof(fileContent));
            Guard.ArgumentIsNotNullOrEmpty(prompt, nameof(prompt));
            
            string assistantId = Guid.NewGuid().ToString();

            IAIAssistant assistant = await assistantFactory.CreateAssistant(assistantId);
            Assistants.TryAdd(assistantId, assistant);

            await assistant.InitializeAsync(new OpenAIAssistantOptions($"{assistantId}.xlsx", fileContent) {
                Instructions = prompt,
                UseFileSearchTool = false,
            });

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

        public void DisposeAssistant(string assistantId) {
            Guard.ArgumentIsNotNullOrEmpty(assistantId, nameof(assistantId));

            if(Assistants.TryRemove(assistantId, out IAIAssistant assistant)) {
                assistant.Dispose();
            }
        }
    }
}

