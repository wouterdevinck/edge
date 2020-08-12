using System;
using System.Data.Common;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using iotedgeapiclient;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace huemodule {

    internal class HueIdentityTranslator {

        private readonly string _hueToken;

        private readonly EdgeClient _edge;

        public HueIdentityTranslator(string hueToken, EdgeClient edge) {
            _hueToken = hueToken;
            _edge = edge;
        }

        public async Task ConnectAsync() {
            var moduleClient = await _edge.CreateModuleClientAsync();
            await moduleClient.OpenAsync();
            await moduleClient.SetMethodHandlerAsync("DeviceRegistered", DeviceRegistered, moduleClient);
            var hue = new HueRepository();
            hue.DeviceDiscovered += async device => {
                await RegisterLeafDevice(moduleClient, device.IotDeviceId);
                device.DeviceUpdate += update => Console.WriteLine($"Device {device.IotDeviceId} updated {update}");
            };
            await hue.ConnectAsync(_hueToken);
        }

        private async Task RegisterLeafDevice(ModuleClient moduleClient, string id) {
            var request = new RegistrationRequest(id);
            var requestJson = JsonConvert.SerializeObject(request);
            using var registrationMessage = new Message(Encoding.UTF8.GetBytes(requestJson)) {
                ContentEncoding = "utf-8",
                ContentType = "application/json"
            };
            registrationMessage.Properties.Add("type", "DeviceRegistration");
            await moduleClient.SendEventAsync("output", registrationMessage);
            Console.WriteLine($"Registering leaf device '{id}' with IoTHub.");
        }

        private async Task<MethodResponse> DeviceRegistered(MethodRequest methodRequest, object userContext) {
            if (!(userContext is ModuleClient moduleClient)) {
                throw new InvalidOperationException("UserContext doesn't contain expected values");
            }
            var methodResponse = new MethodResponse(200);
            var response = JsonConvert.DeserializeObject<RegistrationResponse>(methodRequest.DataAsJson);
            var leafDeviceId = response.LeafDeviceId;
            Console.WriteLine($"Leaf device '{leafDeviceId}' registered with IoTHub");
            var dc = await _edge.CreateDeviceClientAsync(leafDeviceId);
            dc.SetConnectionStatusChangesHandler((status, reason) => {
                Console.WriteLine($"Device connection status for {leafDeviceId} changed to {status}, because of {reason}.");
            });
            await dc.OpenAsync();
            return methodResponse;
        }

    }

}
