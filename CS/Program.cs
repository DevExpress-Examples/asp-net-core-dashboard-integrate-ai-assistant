using Azure;
using Azure.AI.OpenAI;
using DashboardAIAssistant;
using DashboardAIAssistant.Services;
using DevExpress.AIIntegration;
using DevExpress.AspNetCore;
using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardCommon;
using DevExpress.DashboardWeb;
using DevExpress.DataAccess.Excel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services
               .AddResponseCompression()
               .AddDevExpressControls()
               .AddDistributedMemoryCache()
               .AddSession()
               .AddControllers();

builder.Services.AddScoped((IServiceProvider serviceProvider) => {
    DashboardConfigurator configurator = new DashboardConfigurator();
    configurator.SetConnectionStringsProvider(new DashboardConnectionStringsProvider(builder.Configuration));

    DashboardFileStorage dashboardFileStorage = new DashboardFileStorage(builder.Environment.ContentRootFileProvider.GetFileInfo("Data/Dashboards").PhysicalPath);
    configurator.SetDashboardStorage(dashboardFileStorage);

    DataSourceInMemoryStorage dataSourceStorage = new DataSourceInMemoryStorage();

    // Registers an Excel data source.
    DashboardExcelDataSource excelDataSource = new DashboardExcelDataSource("Excel Data Source");
    excelDataSource.FileName = builder.Environment.ContentRootFileProvider.GetFileInfo("Data/Sales.xlsx").PhysicalPath;
    excelDataSource.SourceOptions = new ExcelSourceOptions(new ExcelWorksheetSettings("Sheet1"));
    dataSourceStorage.RegisterDataSource("excelDataSource", excelDataSource.SaveToXml());

    configurator.SetDataSourceStorage(dataSourceStorage);

    return configurator;
});

builder.Services.AddRazorPages();

var azureOpenAIClient = new AzureOpenAIClient(
    new Uri(EnvSettings.AzureOpenAIEndpoint),
    new AzureKeyCredential(EnvSettings.AzureOpenAIKey));

var chatClient = azureOpenAIClient.AsChatClient(EnvSettings.DeploymentName);

builder.Services.AddSingleton(chatClient);
builder.Services.AddSingleton<IAIAssistantProvider, AIAssistantProvider>();
builder.Services.AddDevExpressAI(config =>
{
    config.RegisterOpenAIAssistants(azureOpenAIClient, EnvSettings.DeploymentName);
});

builder.Services.AddScoped<AspNetCoreDashboardExporter>();

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);

var app = builder.Build();

if(app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
} else {
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseDevExpressControls();
app.UseSession();
app.UseRouting();
app.MapDashboardRoute("dashboardControl", "DefaultDashboard");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
