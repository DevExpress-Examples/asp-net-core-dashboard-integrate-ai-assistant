using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Agents;
using DevExpress.AIIntegration.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Files;
using OpenAI.Responses;

namespace DashboardAIAssistant.Services {
    // The OpenAI.Responses API is for evaluation purposes only and is subject to change or removal in a future update.
    // The following code suppresses the OPENAI001 diagnostic.
#pragma warning disable OPENAI001
    public class AgentFactory {
        readonly AzureOpenAIClient openAIClient;
        readonly string deployment;
        readonly ILogger<AgentFactory> logger;

        public AgentFactory(AzureOpenAIClient openAIClient, string deployment, ILogger<AgentFactory> logger) {
            this.openAIClient = openAIClient;
            this.deployment = deployment;
            this.logger = logger;
        }

        // Upload an Excel stream to OpenAI and return an IChatResponseProvider backed
        // by a Responses API agent with a Code Interpreter tool.
        // The cleanup delegate removes the uploaded file when the chat session ends.
        public async Task<(IChatResponseProvider Provider, Func<Task> Cleanup)> CreateChatProviderAsync(
            Stream data, string fileName, string instructions, CancellationToken ct = default) {

            var fileClient = openAIClient.GetOpenAIFileClient();
            var responsesClient = openAIClient.GetResponsesClient();

            if(data.CanSeek)
                data.Position = 0;

            var file = (await fileClient.UploadFileAsync(data, fileName, FileUploadPurpose.Assistants, ct)).Value;

            var tools = new List<AITool> {
                new HostedCodeInterpreterTool { Inputs = [new HostedFileContent(file.Id)] }
            };

            var aiAgent = responsesClient.AsAIAgent(
                instructions: instructions,
                tools: tools,
                name: $"Dashboard Agent {Guid.NewGuid():N}",
                model: deployment);

            // Create a session so the assistant remembers message history and can answer follow-up questions in context.
            var session = await aiAgent.CreateSessionAsync(ct);
            var provider = aiAgent.AsIChatResponseProvider(session);

            async Task Cleanup() {
                try { await fileClient.DeleteFileAsync(file.Id); }
                catch(Exception ex) { logger.LogError(ex, "Error deleting file {Id}", file.Id); }
            }

            return (provider, Cleanup);
        }
    }
#pragma warning restore OPENAI001
}
