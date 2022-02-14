using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;

namespace DeadLockFunction
{
    public static class Function1
    {
        static object object1 = new object();
        static object object2 = new object();

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //========================
            // Non-Template Code for Deadlock

            // Creating a new thread that is not monitored by the parent, or calling thread
            // for completion.  Meaning the parent thread does not care if the child thread finishes
            // or fails, the thread will started and then left alone
            for (int x = 0; x < 100; x++)
            {
                Task.Factory.StartNew(async () => {
                    await StartDeadlock();
                });
            }

            //========================

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        public static Task StartDeadlock()
        {
            Thread thread1 = new Thread((ThreadStart)ObliviousFunction);
            Thread thread2 = new Thread((ThreadStart)BlindFunction);

            thread1.Start();
            thread2.Start();

            while (true)
            {
                // Stare at the two threads in deadlock.
            }
        }

        public static void ObliviousFunction()
        {
            lock (object1)
            {
                Thread.Sleep(1000); // Wait for the blind to lead
                lock (object2)
                {
                }
            }
        }

        public static void BlindFunction()
        {
            lock (object2)
            {
                Thread.Sleep(1000); // Wait for oblivion
                lock (object1)
                {
                }
            }
        }
    }
}
