using DevExpress.AIIntegration.Services.Assistant;
using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardCommon;
using DevExpress.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DashboardAIAssistant.Services;
using System.IO;
using System.Threading.Tasks;

namespace DashboardAIAssistant.Controllers {
    public class AIChatController : Controller {
        private readonly IAIAssistantProvider aiChatService;
        private readonly AspNetCoreDashboardExporter exporter;
        private readonly IHttpContextAccessor contextAccessor;

        public AIChatController(AspNetCoreDashboardExporter exporter, IAIAssistantProvider aiChatService, IHttpContextAccessor contextAccessor) {
            this.exporter = exporter;
            this.aiChatService = aiChatService;
            this.contextAccessor = contextAccessor;
        }

        [HttpPost]
        public async Task<string> CreateChat([FromForm] string dashboardId, [FromForm] string dashboardState) {
            Guard.ArgumentIsNotNullOrEmpty(dashboardId, nameof(dashboardId));

            using(MemoryStream ms = new MemoryStream()) {
                DashboardState state = new DashboardState();
                state.LoadFromJson(dashboardState);

                exporter.ExportToExcel(dashboardId, ms, state, new DashboardExcelExportOptions() { ExportParameters = true, ExportFilters = true });

                return await aiChatService.CreateAssistant(ms, AssistantHelper.Prompt);
            }
        }

        [HttpPost]
        public async Task<string> GetAnswer([FromForm] string chatId, [FromForm] string question) {
            Guard.ArgumentIsNotNullOrEmpty(chatId, nameof(chatId));

            IAIAssistant assistant = aiChatService.GetAssistant(chatId);
            return await assistant.GetAnswerAsync(question);
        }

        [HttpGet]
        public IActionResult CloseChat([FromQuery] string chatId) {
            Guard.ArgumentIsNotNullOrEmpty(chatId, nameof(chatId));

            aiChatService.DisposeAssistant(chatId);

            return new OkResult();
        }
    }
}
