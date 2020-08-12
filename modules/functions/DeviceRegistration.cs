using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace functions {

    public static class DeviceRegistration {

        // TODO Should not be reported as successful execution in case of error 

        [FunctionName("DeviceRegistration")]
        public static async Task Run([EventGridTrigger] EventGridEvent ev, ILogger log) {

            // Get settings from environment
            var hub = Environment.GetEnvironmentVariable("IoTHubConnectionString", EnvironmentVariableTarget.Process);

            // Log the event
            var json = ev.Data.ToString();
            log.LogInformation(json);

            // Sanity check
            if (json == null) {
                log.LogError("Invalid input - json is null");
                return;
            }

            // Parse event parameters
            var data = JObject.Parse(json);
            var leafDeviceId = data["body"]?["LeafDeviceId"]?.ToString();
            var edgeDeviceId = data["systemProperties"]?["iothub-connection-device-id"]?.ToString();
            var edgeModuleId = data["systemProperties"]?["iothub-connection-module-id"]?.ToString();
            if (edgeModuleId == null || edgeDeviceId == null || leafDeviceId == null) {
                log.LogError("Invalid input - missing parameters");
                return;
            }
            log.LogInformation($"DeviceId: {leafDeviceId}");
            log.LogInformation($"Parent device id: {edgeDeviceId}");
            log.LogInformation($"Parent module id: {edgeModuleId}");

            // Get the edge device and edge module details from IoT Hub
            var registryManager = RegistryManager.CreateFromConnectionString(hub);
            var parentDevice = await registryManager.GetDeviceAsync(edgeDeviceId);
            var parentModule = await registryManager.GetModuleAsync(edgeDeviceId, edgeModuleId);
            if (parentDevice == null || parentModule == null) {
                log.LogError("Module or parent device could not be found.");
                return;
            } 

            // Calculate keys for the leaf device, derived from module keys
            var parentPrimary = Convert.FromBase64String(parentModule.Authentication.SymmetricKey.PrimaryKey);
            var parentSecondary = Convert.FromBase64String(parentModule.Authentication.SymmetricKey.SecondaryKey);
            var deviceSymmetricKey = ComputeDerivedSymmetricKey(parentPrimary, leafDeviceId);
            var deviceSecondaryKey = ComputeDerivedSymmetricKey(parentSecondary, leafDeviceId);

            // Construct new device
            var newDevice = new Device(leafDeviceId) {
                Authentication = new AuthenticationMechanism {
                    SymmetricKey = new SymmetricKey {
                        PrimaryKey = deviceSymmetricKey,
                        SecondaryKey = deviceSecondaryKey
                    }
                },
                Scope = parentDevice.Scope
            };

            // Add new device or update it if it already exists
            try {
                await registryManager.AddDeviceAsync(newDevice);
                log.LogWarning($"New device {leafDeviceId} added");
            } catch (DeviceAlreadyExistsException) {
                log.LogWarning($"Device {leafDeviceId} already exists, updating keys and enabling");
                var leafDevice = await registryManager.GetDeviceAsync(leafDeviceId);
                leafDevice.Authentication = new AuthenticationMechanism {
                    SymmetricKey = new SymmetricKey() {
                        PrimaryKey = deviceSymmetricKey,
                        SecondaryKey = deviceSecondaryKey
                    }
                };
                leafDevice.Scope = parentDevice.Scope;
                leafDevice.Status = DeviceStatus.Enabled;
                await registryManager.UpdateDeviceAsync(leafDevice);
            } catch (Exception exception) {
                log.LogError($"Exception when adding leaf device: {exception.Message}");
                return;
            }

            // Invoke the "DeviceRegistered" direct method on the module
            var serviceClient = ServiceClient.CreateFromConnectionString(hub);
            var methodInvocation = new CloudToDeviceMethod("DeviceRegistered") {
                ResponseTimeout = TimeSpan.FromSeconds(30)
            };
            methodInvocation.SetPayloadJson(JsonConvert.SerializeObject(new {LeafDeviceId = leafDeviceId}));
            try {
                var response = await serviceClient.InvokeDeviceMethodAsync(edgeDeviceId, edgeModuleId, methodInvocation);
                log.LogInformation($"Response status: {response.Status}, payload: {response.GetPayloadAsJson()}");
            } catch {
                log.LogWarning($"Error in calling DM 'DeviceRegistered' to module '{edgeModuleId}'");
            }

            // Done
            log.LogInformation("Finished processing DeviceRegistration function");

        }

        private static string ComputeDerivedSymmetricKey(byte[] masterKey, string registrationId) {
            using var hmac = new HMACSHA256(masterKey);
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
        }
        
    }

}