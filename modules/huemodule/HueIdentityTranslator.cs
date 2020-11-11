using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace huemodule {

    internal class HueIdentityTranslator {

        private readonly string _hueToken;

        private readonly EdgeClient _edge;
        private readonly HueRepository _hue;

        public HueIdentityTranslator(string hueToken, EdgeClient edge) {
            _hueToken = hueToken;
            _edge = edge;
            _hue = new HueRepository();
        }

        public async Task ConnectAsync() {
            var moduleClient = await _edge.CreateModuleClientAsync();
            await moduleClient.OpenAsync();
            await moduleClient.SetMethodHandlerAsync("DeviceRegistered", DeviceRegistered, moduleClient);
            _hue.DeviceDiscovered += async device => {
                await RegisterLeafDevice(moduleClient, device.IotDeviceId);
                device.DeviceUpdate += update => Console.WriteLine($"Device {device.IotDeviceId} updated {update}");
            };
            await _hue.ConnectAsync(_hueToken);
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
            if (!(userContext is ModuleClient)) {
                throw new InvalidOperationException("UserContext doesn't contain expected values");
            }
            var methodResponse = new MethodResponse(200);
            var response = JsonConvert.DeserializeObject<RegistrationResponse>(methodRequest.DataAsJson);
            var leafDeviceId = response.LeafDeviceId;
            Console.WriteLine($"Leaf device '{leafDeviceId}' registered with IoTHub");
            var device = _hue.GetDeviceById(leafDeviceId);
            var client = await _edge.CreateDeviceClientAsync(leafDeviceId);
            var wrapper = new HueIotDevice(device, client);
            await wrapper.ConnectAsync();
            return methodResponse;
        }

    }

}
