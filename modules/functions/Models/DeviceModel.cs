using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace functions.Models {

    internal class DeviceModel {

        [JsonProperty("deviceId")] 
        public string DeviceId { get; set; }

        [JsonProperty("modelId", NullValueHandling = NullValueHandling.Ignore)] 
        public string ModelId { get; }

        [JsonProperty("connected", NullValueHandling = NullValueHandling.Ignore)] 
        public bool? Connected { get; }

        [JsonProperty("twin")] 
        public TwinModel TwinModel { get; }

        public DeviceModel(Twin twin) {
            DeviceId = twin.DeviceId;
            if (!string.IsNullOrEmpty(twin.ModelId)) {
                ModelId = twin.ModelId;
            }
            if (twin.ConnectionState != null) { 
                Connected = twin.ConnectionState == DeviceConnectionState.Connected;
            }
            TwinModel = new TwinModel(twin);
        }

    }

}