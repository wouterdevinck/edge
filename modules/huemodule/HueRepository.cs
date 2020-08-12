using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Q42.HueApi;

namespace huemodule {

    internal class HueRepository {

        // TODO Make this configurable as environment variables
        private const uint MinPullIntervalMs = 1000;
        private const uint MaxPullIntervalMs = 5 * 60 * 1000;
        private const uint BackOffIntervalMs = 30 * 1000;
        private const uint BackOffFactor = 2;

        public event Action<HueDevice> DeviceDiscovered;

        private HueClient _hue;
        private List<HueDevice> _devices;

        private Timer _timer;
        private DateTime _backOffTime;

        public HueRepository() {
            _devices = new List<HueDevice>();
        }

        public async Task ConnectAsync(string token) {

            // Discover bridges
            var locator = new HttpBridgeLocator();
            var bridges = (await locator.LocateBridgesAsync(TimeSpan.FromMinutes(1))).ToList();
            if (bridges.Count == 0) throw new Exception("No bridges found");
            if (bridges.Count > 1) throw new Exception("Multiple bridges found, not supported");
            var bridgeInfo = bridges.Single();

            // Connect to bridge
            _hue = new LocalHueClient(bridgeInfo.IpAddress);
            _hue.Initialize(token);

            // Get all devices from the bridge
            _devices = await GetAllDevices();

            // Fire event for each device
            foreach (var device in _devices) {
                DeviceDiscovered?.Invoke(device);
            }

            // Periodically check if new devices have been added
            _backOffTime = DateTime.Now + TimeSpan.FromMilliseconds(BackOffIntervalMs);
            _timer = new Timer(MinPullIntervalMs);
            _timer.Elapsed += OnUpdate;
            _timer.AutoReset = true;
            _timer.Enabled = true;

        }

        private async void OnUpdate(object source, ElapsedEventArgs e) {

            // Back off exponentially
            if (DateTime.Now >= _backOffTime) {
                _timer.Interval = Math.Min(_timer.Interval * BackOffFactor, MaxPullIntervalMs);
                _backOffTime = DateTime.Now + TimeSpan.FromMilliseconds(BackOffIntervalMs);
            }

            // Get all devices and calculate the delta
            var allDevices = await GetAllDevices();
            var devices = _devices.ToList();
            var newDevices = allDevices.Where(x => devices.All(y => y.IotDeviceId != x.IotDeviceId)).ToList();
            var existingDevices = allDevices.Where(x => devices.Any(y => y.IotDeviceId == x.IotDeviceId)).ToList();
            var updatedDevices = existingDevices.Where(x => x != devices.Single(y => y.IotDeviceId == x.IotDeviceId)).ToList();

            // Update the repository
            _devices.AddRange(newDevices);
            foreach (var device in updatedDevices) {
                _devices.Single(x => x.IotDeviceId == device.IotDeviceId).Update(device);
            }

            // Fire events for new devices
            foreach (var device in newDevices) {
                DeviceDiscovered?.Invoke(device);
            }

            // In case there were any updates, go back to faster polling
            if (updatedDevices.Any() || newDevices.Any()) {
                FastUpdate();
            }

            // Log some info
            // Console.WriteLine($"Polled Hue bridge at {DateTime.Now}. " +
            //                   $"Total of {allDevices.Count} devices, {newDevices.Count} new and {updatedDevices.Count} updated. " +
            //                   $"Next poll in {_timer.Interval}ms. ");

        }

        private async Task<List<HueDevice>> GetAllDevices() {
            var devices = new List<HueDevice>();
            var bridge = await _hue.GetBridgeAsync();
            var lights = await _hue.GetLightsAsync();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var sensors = (await _hue.GetSensorsAsync()).Where(x => x.Capabilities != null);
            devices.Add(new HueDevice(bridge));
            devices.AddRange(lights.Select(x => new HueDevice(x, _hue)));
            devices.AddRange(sensors.Select(x => new HueDevice(x)));
            // var config = await _hue.GetConfigAsync();
            return devices;
        }

        public HueDevice GetDeviceByName(string name) {
            return _devices.Single(x => x.Name == name);
        }

        public void FastUpdate() {
            _timer.Interval = MinPullIntervalMs;
            _backOffTime = DateTime.Now + TimeSpan.FromMilliseconds(BackOffIntervalMs);
        }

    }

}
