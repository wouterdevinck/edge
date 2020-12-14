using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;

namespace functions {

    public static class InvokeCommand {

        [FunctionName("InvokeCommand")]
        public static async Task<IActionResult> Run(
          [HttpTrigger(AuthorizationLevel.Function, "post", Route = "devices/{deviceId}/commands/{command}")] 
          HttpRequest req, string deviceId, string command, ILogger log) {
            var hub = Environment.GetEnvironmentVariable("IoTHubConnectionString", EnvironmentVariableTarget.Process);
            log.LogInformation($"Invoking command {command} on device {deviceId}");
            var serviceClient = ServiceClient.CreateFromConnectionString(hub);
            var commandInvocation = new CloudToDeviceMethod(command) { ResponseTimeout = TimeSpan.FromSeconds(30) };
            try {
                var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, commandInvocation);
                if (result.Status == 200) return new OkResult();
                return new InternalServerErrorResult();
            } catch (Exception) {
                return new InternalServerErrorResult();
            }
        }

    }

}