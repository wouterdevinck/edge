using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace functions.Models {

    internal class DeviceModel {

        [JsonProperty("deviceId")] 
        public string DeviceId { get; }

        [JsonProperty("modelId")] 
        public string ModelId { get; }

        [JsonProperty("connected")] 
        public bool Connected { get; }

        [JsonProperty("twin")] 
        public TwinModel TwinModel { get; }

        public DeviceModel(Twin twin) {
            DeviceId = twin.DeviceId;
            ModelId = twin.ModelId;
            Connected = twin.ConnectionState == DeviceConnectionState.Connected;
            TwinModel = new TwinModel(twin);
        }

    }

}