using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Q42.HueApi;
using Q42.HueApi.Models;

namespace huemodule {

    internal enum HueDeviceType {
        Bridge,
        Light,
        Plug,
        Sensor
    }

    internal class HueDevice {

        // capabilities like min and max temp
        // sw updates (properties? or commands?) - date and status? available/downloading/etc.

        public event Action<string> DeviceUpdate;

        private const string TypePlug = "On/Off plug-in unit";
        private const string TypeDimmable = "Dimmable light";
        private const string TypeColorTemperature = "Color temperature light";

        private readonly HueClient _hue;

        private readonly string _uniqueId;
        private readonly string _localId;

        public string IotDeviceId => $"HUE-{_typeNames[Type]}-{_uniqueId}";

        public HueDeviceType Type { get; private set; }

        private readonly Dictionary<HueDeviceType, string> _typeNames = new Dictionary<HueDeviceType, string> {
            { HueDeviceType.Bridge, "BRIDGE" },
            { HueDeviceType.Light,  "LIGHT"  },
            { HueDeviceType.Plug,   "PLUG"   },
            { HueDeviceType.Sensor, "SENSOR" }
        };

        public string Name { get; private set; }
        public string Manufacturer { get; private set; }
        public string Model { get; private set; }
        public string Version { get; private set; }

        public bool Reachable { get; private set; }
        public bool On { get; private set; }
        public byte Brightness { get; private set; }
        public int ColorTemperature { get; private set; }

        public bool HasOnOff { get; }
        public bool HasBrightness { get; }
        public bool HasTemperature { get; }

        public byte Battery { get; private set; }
        public int ButtonEvent { get; private set; }
        public DateTime SensorUpdate { get; private set; }

        public HueDevice(Light light, HueClient hue) {
            _hue = hue;
            _uniqueId = light.UniqueId;
            _localId = light.Id;
            Type = light.Type == TypePlug ? HueDeviceType.Plug : HueDeviceType.Light;
            Name = light.Name;
            Manufacturer = light.ManufacturerName;
            Model = light.ModelId;
            Version = light.SoftwareVersion;
            if (light.State.IsReachable != null) Reachable = light.State.IsReachable.Value;
            On = light.State.On && light.State.IsReachable == true;
            Brightness = light.State.Brightness;
            if (light.State.ColorTemperature != null) ColorTemperature = light.State.ColorTemperature.Value;
            switch (light.Type) {
                case TypePlug:
                    HasOnOff = true;
                    break;
                case TypeDimmable:
                    HasOnOff = true;
                    HasBrightness = true;
                    break;
                case TypeColorTemperature:
                    HasOnOff = true;
                    HasBrightness = true;
                    HasTemperature = true;
                    break;
                default:
                    Console.WriteLine($"Unknown light type: '{light.Type}' ({light.Name})");
                    break;
            }
            // limited update info?
        }

        public HueDevice(Sensor sensor, HueClient hue) {
            _hue = hue;
            _localId = sensor.Id;
            Type = HueDeviceType.Sensor;
            _uniqueId = sensor.UniqueId;
            Name = sensor.Name;
            Manufacturer = sensor.ManufacturerName;
            Model = sensor.ModelId;
            Version = sensor.SwVersion;
            if (sensor.Config.Reachable != null) Reachable = sensor.Config.Reachable.Value;
            if (sensor.Config.Battery != null) Battery = (byte)sensor.Config.Battery;
            if (sensor.State.ButtonEvent != null) ButtonEvent = sensor.State.ButtonEvent.Value;
            if (sensor.State.Lastupdated != null) SensorUpdate = sensor.State.Lastupdated.Value;
            // no update info?
        }

        public HueDevice(Bridge bridge, HueClient hue) {
            _hue = hue;
            Type = HueDeviceType.Bridge;
            _uniqueId = bridge.Config.BridgeId;
            Name = bridge.Config.Name;
            Manufacturer = "Signify Netherlands B.V.";
            Model = bridge.Config.ModelId;
            Version = bridge.Config.SoftwareVersion;
            Reachable = true;
            // var upd = bridge.Config.SoftwareUpdate2;
        }

        public async Task TurnOn() {
            if (!HasOnOff) throw new NotSupportedException();
            var command = new LightCommand { On = true };
            await _hue.SendCommandAsync(command, new List<string> { _localId });
        }

        public async Task TurnOff() {
            if (!HasOnOff) throw new NotSupportedException();
            var command = new LightCommand { On = false };
            await _hue.SendCommandAsync(command, new List<string> { _localId });
        }

        public async Task SetBrightness(byte bri) {
            if (!HasBrightness) throw new NotSupportedException();
            var command = new LightCommand { Brightness = bri };
            await _hue.SendCommandAsync(command, new List<string> { _localId });
        }

        public async Task SetTemperature(int ct) {
            if (!HasTemperature) throw new NotSupportedException();
            var command = new LightCommand { ColorTemperature = ct };
            await _hue.SendCommandAsync(command, new List<string> { _localId });
        }

        public async Task SetName(string name) {
            if (Type == HueDeviceType.Sensor) {
                await _hue.UpdateSensorAsync(_localId, name);
            }
            if (Type == HueDeviceType.Light || Type == HueDeviceType.Plug) {
                await _hue.SetLightNameAsync(_localId, name);
            }
            if (Type == HueDeviceType.Bridge) {
                await _hue.UpdateBridgeConfigAsync(new BridgeConfigUpdate {
                    Name = name
                });
            }
            // TODO Check if success? E.g. name too short 
            Name = name;
        }

        public void Update(HueDevice other) {
            if (_uniqueId != other._uniqueId) throw new Exception("Unique ID mismatch!");
            if (Type != other.Type) {
                DeviceUpdate?.Invoke("Type");
                Type = other.Type;
            }
            if (Name != other.Name) {
                DeviceUpdate?.Invoke("Name");
                Name = other.Name;
            }
            if (Manufacturer != other.Manufacturer) {
                DeviceUpdate?.Invoke("Manufacturer");
                Manufacturer = other.Manufacturer;
            }
            if (Model != other.Model) {
                DeviceUpdate?.Invoke("Model");
                Model = other.Model;
            }
            if (Version != other.Version) {
                DeviceUpdate?.Invoke("Version");
                Version = other.Version;
            }
            if (Reachable != other.Reachable) {
                DeviceUpdate?.Invoke("Reachable");
                Reachable = other.Reachable;
            }
            if (Battery != other.Battery) {
                DeviceUpdate?.Invoke("Battery");
                Battery = other.Battery;
            }
            if (On != other.On) {
                DeviceUpdate?.Invoke("On");
                On = other.On;
            }
            if (Brightness != other.Brightness) {
                DeviceUpdate?.Invoke("Brightness");
                Brightness = other.Brightness;
            }
            if (ColorTemperature != other.ColorTemperature) {
                DeviceUpdate?.Invoke("ColorTemperature");
                ColorTemperature = other.ColorTemperature;
            }
            if (ButtonEvent != other.ButtonEvent) {
                DeviceUpdate?.Invoke("ButtonEvent");
                // Console.WriteLine($"from {ButtonEvent} to {other.ButtonEvent}");
                ButtonEvent = other.ButtonEvent;
            }
            if (SensorUpdate != other.SensorUpdate) {
                DeviceUpdate?.Invoke("SensorUpdate");
                SensorUpdate = other.SensorUpdate;
            }
        }

        protected bool Equals(HueDevice other) {
            return _uniqueId == other._uniqueId &&
                   Type == other.Type &&
                   Name == other.Name &&
                   Manufacturer == other.Manufacturer &&
                   Model == other.Model &&
                   Version == other.Version &&
                   Reachable == other.Reachable &&
                   Battery == other.Battery &&
                   On == other.On &&
                   Brightness == other.Brightness &&
                   ColorTemperature == other.ColorTemperature &&
                   ButtonEvent == other.ButtonEvent &&
                   SensorUpdate == other.SensorUpdate;
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((HueDevice)obj);
        }

        public override int GetHashCode() {
            return HashCode.Combine(_uniqueId);
        }

        public static bool operator ==(HueDevice left, HueDevice right) {
            return Equals(left, right);
        }

        public static bool operator !=(HueDevice left, HueDevice right) {
            return !Equals(left, right);
        }

    }

}
