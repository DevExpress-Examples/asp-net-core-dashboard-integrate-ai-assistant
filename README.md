<!-- default badges list -->
[![](https://img.shields.io/badge/Open_in_DevExpress_Support_Center-FF7200?style=flat-square&logo=DevExpress&logoColor=white)](https://supportcenter.devexpress.com/ticket/details/T1279614)
[![](https://img.shields.io/badge/📖_How_to_use_DevExpress_Examples-e9f6fc?style=flat-square)](https://docs.devexpress.com/GeneralInformation/403183)
[![](https://img.shields.io/badge/💬_Leave_Feedback-feecdd?style=flat-square)](#does-this-example-address-your-development-requirementsobjectives)
<!-- default badges end -->
# Dashboard for ASP.NET Core — Integrate AI Assistant based on Azure OpenAI

This example is an ASP.NET Core application with integrated DevExpress BI Dashboard and an AI assistant. User requests and assistant responses are displayed on-screen using the DevExtreme [`dxChat`](https://js.devexpress.com/jQuery/Documentation/24_2/ApiReference/UI_Components/dxChat/) component. The AI Assistant is implemented as a [custom dashboard item](https://docs.devexpress.com/Dashboard/117546/web-dashboard/advanced-customization/create-a-custom-item) based on the `dxChat` widget.

![DevExpress BI Dashboard - Integrate an AI Assistant](images/dashboard-ai-assistant.png)

The AI Assistant reviews and analyzes all the data displayed in the dashboard to answer your questions. For a more focused analysis, you can select a specific dashboard item. Click the **Select widget** button in the AI Assistant custom item's caption and choose the desired widget. Any changes to dashboard data—such as updates to parameters or master filters—will automatically trigger a recreation of the AI Assistant. The dashboard data is exported in Excel format and passed to the AI Assistant.

**Please note that AI Assistant initialization takes time. The assistant is ready for interaction once Microsoft Azure scans the source document on the server side.**

## Implementation Details

### Add Personal Keys

> [!NOTE]  
> DevExpress AI-powered extensions follow the "bring your own key" principle. DevExpress does not offer a REST API and does not ship any built-in LLMs/SLMs. You need an active Azure/Open AI subscription to obtain the REST API endpoint, key, and model deployment name. These variables must be specified at application startup to register AI clients and enable DevExpress AI-powered Extensions in your application.

Create an Azure OpenAI resource in the Azure portal to use AI Assistants for DevExpress BI Dashboard. Refer to the following help topic for details: [Microsoft - Create and deploy an Azure OpenAI Service resource](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).

Once you obtain a private endpoint and an API key, register them as `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_APIKEY` environment variables. The [EnvSettings.cs](./CS/EnvSettings.cs) reads these settings. `DeploymentName` in this file is a name of your Azure model, for example, `GPT4o`:

```cs
public static class EnvSettings {
    public static string AzureOpenAIEndpoint { get { return Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"); } }
    public static string AzureOpenAIKey { get { return Environment.GetEnvironmentVariable("AZURE_OPENAI_APIKEY"); } }
    public static string DeploymentName { get { return "GPT4o"; } }
}
```

Files to Review: 
- [EnvSettings.cs](./CS/EnvSettings.cs)

### Register AI Services

Register AI services in your application. Add the following code to the _Program.cs_ file:

```cs
using DevExpress.AIIntegration;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using System;
// ...
var azureOpenAIClient = new AzureOpenAIClient(
    new Uri(EnvSettings.AzureOpenAIEndpoint),
    new AzureKeyCredential(EnvSettings.AzureOpenAIKey));

var chatClient = azureOpenAIClient.AsChatClient(EnvSettings.DeploymentName);

builder.Services.AddDevExpressAI(config =>
{
    config.RegisterOpenAIAssistants(azureOpenAIClient, EnvSettings.DeploymentName);
});
// ...
```

>[!NOTE]
> Availability of Azure Open AI Assistants depends on the region. Refer to the following article for more details: [Assistants (Preview)](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models?tabs=global-standard%2Cstandard-chat-completions#assistants-preview).

Files to Review: 
- [Program.cs](./CS/Program.cs)

### AI Assistant Provider
 
On the server side, the `AIAssistantProvider` service manages assistants. An `IAIAssistantFactory` instance creates assistants with keys specified in previous steps.
 
```cs 
public interface IAIAssistantProvider {
    IAIAssistant GetAssistant(string assistantName);
    Task<string> CreateAssistant(AssistantType assistantType, Stream data);
    Task<string> CreateAssistant(AssistantType assistantType);
    void DisposeAssistant(string assistantName);
}
```

You can review and tailor AI assistant instructions in the following file: [AssistantHelper.cs](./CS/Services/AssistantHelper.cs)

Files to Review: 
- [AIAssistantProvider.cs](./CS/Services/AIAssistantProvider.cs)
- [IAIAssistantProvider.cs](./CS/IAIAssistantProvider.cs)
- [AssistantHelper.cs](./CS/Services/AssistantHelper.cs)


### Create an AI Assistant Custom Item

This example implements a [custom item](https://docs.devexpress.com/Dashboard/117546/web-dashboard/advanced-customization/create-a-custom-item) based on the [`dxChat`](https://js.devexpress.com/jQuery/Documentation/Guide/UI_Components/Chat/Overview/) component.

For instructions on how to implement custom dashboard items, refer to tutorials in the following section: [Create a Custom Item for the Web Dashboard](https://docs.devexpress.com/Dashboard/117546/web-dashboard/advanced-customization/create-a-custom-item).

For the **AI Assistant** custom item implementation, review to the following file: [aiChatCustomItem.js](./CS/wwwroot/js/aiChatCustomItem.js)

The additional logic for the custom item is implemented in the [Index.cshtml](./CS/Pages/Index.cshtml) file. The `itemCaptionToolbarUpdated` event is used to add a **Select Widget** button to the item's caption. This button allows users to select a dashboard item for the AI Assistant. The `DashboardInitialized` event is handled to implement the *one AI Assistant per dashboard* logic.

Files to Review:
- [Index.cshtml](./CS/Pages/Index.cshtml)
- [aiChatCustomItem.js](./CS/wwwroot/js/aiChatCustomItem.js)

### Register the Custom Item Extension

Register the created custom item extension in the Web Dashboard:

```html
<script type="text/javascript">
    // ...
    function handleBeforeRender(dashboardControl) {
        chatAIItem = new AIChatItem(dashboardControl);
        dashboardControl.registerExtension(chatAIItem);
            // ...
    }
    // ...
</script>

<div style="position: relative; height: calc(100vh - 55px);">
@(Html.DevExpress().Dashboard("dashboardControl1")
    .ControllerName("DefaultDashboard")
    .OnBeforeRender("handleBeforeRender")
    .OnDashboardInitialized("handleDashboardInitialized")
)
</div>
```

After you registered the extension the AI Assistant icon appears in the Dashboard Toolbox: 

![DevExpress BI Dashboard - AI Assistant Custom Item Icon](images/dashboard-toolbar-ai-assistant-item.png)

Click the item to add an AI Assistant item to the dashboard. Only one AI Assistant item is available per dashboard. You can ask the assistant questions in the Viewer mode.

File to Review: 
- [Index.cshtml](./CS/Pages/Index.cshtml)

### Access the Assistant

Each time a dashboard is initialized or its [dashboard state](https://docs.devexpress.com/Dashboard/DevExpress.DashboardCommon.DashboardState) changes, the dashboard data is exported in Excel format and a new assistant is created. This way the AI Assistant is always provided with up-to-date data.

Files to Review: 

- [aiChatCustomItem.js](./CS/wwwroot/js/aiChatCustomItem.js)
- [AIAssistantProvider.cs](./CS/Services/AIAssistantProvider.cs)
- [AIChatController](./CS/Controllers/AIChatController.cs)

### Communicate with Assistant

Each time a user sends a message, the [`onMessageEntered`](https://js.devexpress.com/jQuery/Documentation/24_2/ApiReference/UI_Components/dxChat/Configuration/#onMessageEntered) event handler passes the request to the assistant:

```js
// ...
getAnswer(chatId, question) {
    const formData = new FormData();
    formData.append('chatId', chatId);
    formData.append('question', question);
    return this._tryFetch(async () => {
        const response = await fetch('/AIChat/GetAnswer', {
            method: 'POST',
            body: formData
        });
        return await response.text();
    }, 'GetAnswer');
}
// ...
async getAIResponse(question) {
    this.lastUserQuery = question;

    if(!this.chatId)
        this.chatId = await this.createChat(this.dashboardControl.getDashboardId(), this.dashboardControl.getDashboardState());
    if(this.chatId)
        return await this.getAnswer(this.chatId, question);
};
// ...
async onMessageEntered(e) {
    const instance = e.component;
    this.component.option('alerts', []);
    instance.renderMessage(e.message);
    instance.option({ typingUsers: [assistant] });
    const userInput = e.message.text + ((this.model.selectedSheet && "\nDiscuss item " + this.model.selectedSheet)
        || "\nLet's discuss all items");
    const response = await this.getAIResponse(userInput);
    this.renderAssistantMessage(instance, response);
}
```

[`AIChatController.GetAnswer`](./CS/Controllers/AIChatController.cs#L38) receives answers from the assistant.

## Files to Review

- [Program.cs](./CS/Program.cs)
- [Index.cshtml](./CS/Pages/Index.cshtml)
- [aiChatCustomItem.js](./CS/wwwroot/js/aiChatCustomItem.js)
- [AIAssistantProvider.cs](./CS/Services/AIAssistantProvider.cs)
- [IAIAssistantProvider.cs](./CS/IAIAssistantProvider.cs)
- [AIChatController.cs](./CS/Controllers/AIChatController.cs)
- [AssistantHelper.cs](./CS/Services/AssistantHelper.cs)


## Documentation

- [AI Integration](https://docs.devexpress.com/CoreLibraries/405204/ai-powered-extensions)
- [Create a Custom Item for the Web Dashboard](https://docs.devexpress.com/Dashboard/117546/web-dashboard/advanced-customization/create-a-custom-item)
- [Getting Started with JavaScript/jQuery Chat](https://js.devexpress.com/jQuery/Documentation/Guide/UI_Components/Chat/Getting_Started_with_Chat/)

## More Examples

- [DevExtreme Chat - Getting Started](https://github.com/DevExpress-Examples/devextreme-getting-started-with-chat)
- [Reporting for ASP.NET Core - Integrate AI Assistant based on Azure OpenAI](https://github.com/DevExpress-Examples/web-reporting-integrate-ai-assistant)

<!-- feedback -->
## Does this example address your development requirements/objectives?

[<img src="https://www.devexpress.com/support/examples/i/yes-button.svg"/>](https://www.devexpress.com/support/examples/survey.xml?utm_source=github&utm_campaign=asp-net-core-dashboard-integrate-ai-assistant&~~~was_helpful=yes) [<img src="https://www.devexpress.com/support/examples/i/no-button.svg"/>](https://www.devexpress.com/support/examples/survey.xml?utm_source=github&utm_campaign=asp-net-core-dashboard-integrate-ai-assistant&~~~was_helpful=no)

(you will be redirected to DevExpress.com to submit your response)
<!-- feedback end -->
