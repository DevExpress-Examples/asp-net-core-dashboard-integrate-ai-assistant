using System.IO;
using System.Text;
using System.Threading.Tasks;
using DashboardAIAssistant.Services;
using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardCommon;
using DevExpress.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace DashboardAIAssistant.Controllers {
    public class AIChatController : Controller {
        private readonly IAIDashboardChatService chatService;
        private readonly AspNetCoreDashboardExporter exporter;

        public AIChatController(AspNetCoreDashboardExporter exporter, IAIDashboardChatService chatService) {
            this.exporter = exporter;
            this.chatService = chatService;
        }

        [HttpPost]
        public async Task<string> CreateChat([FromForm] string dashboardId, [FromForm] string dashboardState) {
            Guard.ArgumentIsNotNullOrEmpty(dashboardId, nameof(dashboardId));

            using(MemoryStream ms = new MemoryStream()) {
                DashboardState state = new DashboardState();
                state.LoadFromJson(dashboardState);

                exporter.ExportToExcel(dashboardId, ms, state, new DashboardExcelExportOptions() { ExportParameters = true, ExportFilters = true });

                return await chatService.OpenChatAsync(ms);
            }
        }

        [HttpPost]
        public async Task<string> GetAnswer([FromForm] string chatId, [FromForm] string question) {
            Guard.ArgumentIsNotNullOrEmpty(chatId, nameof(chatId));

            var provider = chatService.GetChatProvider(chatId);

            var sb = new StringBuilder();
            await foreach(var update in provider.GetResponseAsync(
                [new ChatMessage(ChatRole.User, question)], useStreaming: false))
                sb.Append(update.Text);

            return sb.ToString();
        }

        [HttpGet]
        public async Task<IActionResult> CloseChat([FromQuery] string chatId) {
            Guard.ArgumentIsNotNullOrEmpty(chatId, nameof(chatId));

            await chatService.CloseChatAsync(chatId);

            return new OkResult();
        }
    }
}
