﻿using System;
using System.IO;
using System.Threading.Tasks;

using Cognite.Sdk.Api;


namespace csharp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var apiKey = Environment.GetEnvironmentVariable("API_KEY");
            var project = Environment.GetEnvironmentVariable("PROJECT");

            var ctx = new Context();
            ctx
                .AddHeader("api-key", apiKey)
                .SetProject(project);
        }
    }
}
