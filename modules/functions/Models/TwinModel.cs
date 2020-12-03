using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace functions.Models {

    internal class TwinModel {

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; }

        public TwinModel(Twin twin) {
            var reported = twin.Properties.Reported;
            if (reported.Contains("name")) Name = reported["name"];
            if (reported.Contains("version")) Version = reported["version"];
        }

    }

}
