using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace functions {

    public static class ListDevices {

        [FunctionName("ListDevices")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "devices")] HttpRequest req, ILogger log) {
            var hub = Environment.GetEnvironmentVariable("IoTHubConnectionString", EnvironmentVariableTarget.Process);
            var registryManager = RegistryManager.CreateFromConnectionString(hub);
            var devices = await registryManager.CreateQuery("SELECT * FROM devices").GetNextAsTwinAsync();
            return new OkObjectResult(JsonConvert.SerializeObject(devices));
        }

    }

}