﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace TranslatorApp
{
    public static class HelloSequence
    {
        [FunctionName("E1_HelloSequence")]
        public static async Task<List<string>> Run(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>
            {
                await context.CallFunctionAsync<string>("E1_SayHello", "Tokyo"),
                await context.CallFunctionAsync<string>("E1_SayHello", "Seattle"),
                await context.CallFunctionAsync<string>("E1_SayHello", "London")
            };


            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("E1_SayHello")]
        public static string SayHello([ActivityTrigger] string name)
        {
            return $"Hello {name}!";
        }
    }
}
