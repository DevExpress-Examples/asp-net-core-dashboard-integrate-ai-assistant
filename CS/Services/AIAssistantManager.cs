using System;
using System.ClientModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Files;

namespace DashboardAIAssistant.Services {
#pragma warning disable OPENAI001
    public class AIAssistantData(string assistantId, string threadId, string fileId) {
        public string AssistantId { get; } = assistantId;
        public string ThreadId { get; } = threadId;
        public string FileId { get; } = fileId;
    }

    public class AIAssistantManager {
        readonly AssistantClient assistantClient;
        readonly OpenAIFileClient fileClient;
        readonly ILogger<AIAssistantManager> logger;
        readonly string deployment;

        public AIAssistantManager(OpenAIClient client, string deployment, ILogger<AIAssistantManager> logger) {
            assistantClient = client.GetAssistantClient();
            fileClient = client.GetOpenAIFileClient();
            this.deployment = deployment;
            this.logger = logger;
        }

        public async Task<AIAssistantData> CreateAssistantAndThreadAsync(Stream data, string fileName, string instructions, CancellationToken ct = default) {
            data.Position = 0;

            ClientResult<OpenAIFile> fileResponse = await fileClient.UploadFileAsync(data, fileName, FileUploadPurpose.Assistants, ct);
            var file = fileResponse.Value;

            var resources = new ToolResources() {
                CodeInterpreter = new CodeInterpreterToolResources()
            };
            resources.CodeInterpreter.FileIds.Add(file.Id);

            AssistantCreationOptions assistantCreationOptions = new AssistantCreationOptions() {
                Name = Guid.NewGuid().ToString(),
                Instructions = instructions,
                ToolResources = resources,
                Tools = { new CodeInterpreterToolDefinition() }
            };
            

            ClientResult<Assistant> assistantResponse = await assistantClient.CreateAssistantAsync(deployment, assistantCreationOptions, ct);
            var assistant = assistantResponse.Value;
            ClientResult<AssistantThread> threadResponse = await assistantClient.CreateThreadAsync(cancellationToken: ct);
            var thread = threadResponse.Value;

            return new(assistant.Id, thread.Id, file.Id);
        }
        
        public async Task CleanUpAssistantAsync(AIAssistantData assistantData) {
            try{
                if(assistantData != null){
                    await assistantClient.DeleteAssistantAsync(assistantData.AssistantId);
                    await assistantClient.DeleteThreadAsync(assistantData.ThreadId);
                    await fileClient.DeleteFileAsync(assistantData.FileId);
                }
            }
            catch(Exception e) {
                logger.LogError($"Error cleaning up assistant: {e.Message}\n{e.StackTrace}");
            }
        }
    }
#pragma warning restore OPENAI001
}
