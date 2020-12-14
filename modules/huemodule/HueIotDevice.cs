using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace huemodule {

    internal class HueIotDevice {

        private readonly HueDevice _hue;
        private readonly DeviceClient _iot;

        public HueIotDevice(HueDevice hue, DeviceClient iot) {
            _hue = hue;
            _iot = iot;
        }

        public async Task ConnectAsync() {
            _iot.SetConnectionStatusChangesHandler((status, reason) => {
                Console.WriteLine($"Device connection status for {_hue.IotDeviceId} changed to {status}, because of {reason}.");
            });
            await _iot.SetMethodHandlerAsync("on", async (req, ctx) => {
                await _hue.TurnOn();
                return new MethodResponse(200);
            }, null);
            await _iot.SetMethodHandlerAsync("off", async (req, ctx) => {
                await _hue.TurnOff();
                return new MethodResponse(200);
            }, null);
            await UpdateReportedState();
            await _iot.SetDesiredPropertyUpdateCallbackAsync(async (desired, ctx) => {
                var targetName = desired["name"].ToString();
                await _hue.SetName(targetName);
                var reported = new TwinCollection {
                    ["name"] = new TwinCollection {
                        ["value"] = _hue.Name,
                        ["ac"] = 200,
                        ["av"] = desired.Version,
                        ["ad"] = "desired property received"
                    }
                };
                await _iot.UpdateReportedPropertiesAsync(reported);
            }, null);
            _hue.DeviceUpdate += async _ => {
                await UpdateReportedState();
            };
            await _iot.OpenAsync();
        }

        private async Task UpdateReportedState(){
            var reportedProperties = new TwinCollection {
                ["name"] = _hue.Name,
                ["manufacturer"] = _hue.Manufacturer,
                ["model"] = _hue.Model,
                ["version"] = _hue.Version,
                ["reachable"] = _hue.Reachable,
                ["on"] = _hue.On,
                ["brightness"] = _hue.Brightness,
                ["colorTemperature"] = _hue.ColorTemperature,
                ["battery"] = _hue.Battery,
                ["buttonEvent"] = _hue.ButtonEvent
            };
            await _iot.UpdateReportedPropertiesAsync(reportedProperties);
        }

    }

}
