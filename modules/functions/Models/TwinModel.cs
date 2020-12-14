using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace functions.Models {

    internal class TwinModel {

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; }

        [JsonProperty("manufacturer", NullValueHandling = NullValueHandling.Ignore)]
        public string Manufacturer { get; }

        [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model { get; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; }

        [JsonProperty("reachable", NullValueHandling = NullValueHandling.Ignore)]
        public bool Reachable { get; }

        [JsonProperty("on", NullValueHandling = NullValueHandling.Ignore)]
        public bool On { get; }

        [JsonProperty("brightness", NullValueHandling = NullValueHandling.Ignore)]
        public int Brightness { get; }

        [JsonProperty("colorTemperature", NullValueHandling = NullValueHandling.Ignore)]
        public int ColorTemperature { get; }

        [JsonProperty("battery", NullValueHandling = NullValueHandling.Ignore)]
        public int Battery { get; }

        [JsonProperty("buttonEvent", NullValueHandling = NullValueHandling.Ignore)]
        public int ButtonEvent { get; }

        public TwinModel(Twin twin) {
            var reported = twin.Properties.Reported;
            if (reported.Contains("name")) Name = reported["name"];
            if (reported.Contains("manufacturer")) Manufacturer = reported["manufacturer"];
            if (reported.Contains("model")) Model = reported["model"];
            if (reported.Contains("version")) Version = reported["version"];
            if (reported.Contains("reachable")) Reachable = reported["reachable"];
            if (reported.Contains("on")) On = reported["on"];
            if (reported.Contains("brightness")) Brightness = reported["brightness"];
            if (reported.Contains("colorTemperature")) ColorTemperature = reported["colorTemperature"];
            if (reported.Contains("battery")) Battery = reported["battery"];
            if (reported.Contains("buttonEvent")) ButtonEvent = reported["buttonEvent"];
        }

    }

}
