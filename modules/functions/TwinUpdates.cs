using System.Collections.Generic;
using System.Threading.Tasks;
using functions.Models;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json;

namespace functions {

    public static class TwinUpdates {

        [FunctionName("TwinUpdates")]
        public static Task Run([ServiceBusTrigger("twinupdates", Connection = "ServiceBusConnection")] string json, IDictionary<string, object> userProperties, [SignalR(HubName = "notifications")]IAsyncCollector<SignalRMessage> signalr) {
            var twin = JsonConvert.DeserializeObject<Twin>(json);
            var device = new DeviceModel(twin);
            device.DeviceId = userProperties["deviceId"].ToString();
            return signalr.AddAsync(
                new SignalRMessage {
                    Target = "twinupdates",
                    Arguments = new [] { device }
                });
        }

    }

}