﻿using System;

namespace DashboardAIAssistant
{
    public static class EnvSettings
    {
        public static string AzureOpenAIEndpoint { get { return Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"); } }
        public static string AzureOpenAIKey { get { return Environment.GetEnvironmentVariable("AZURE_OPENAI_APIKEY"); } }
        public static string DeploymentName { get { return "GPT4o"; } }
    }
}
