using System;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using iotedgeapiclient;
using Microsoft.Azure.Devices.Client;

namespace huemodule {

    internal class EdgeClient {

        private const string ModelId = "dtmi:wouterdevinck:light;1"; // TODO Not everything is a light - split up models?

        private readonly ITransportSettings[] _transportSettings;

        private readonly string _sasToken;

        private readonly string _generationId;
        private readonly string _edgeModuleId;
        private readonly string _workloadUri;

        private readonly bool _useWorkloadApi = false;

        private readonly string _iotHubHostName;
        private readonly string _gatewayHostName;
        
        private EdgeClient(string iotHubHostName, string gatewayHostName, string certPath, string sasToken) {
            _iotHubHostName = iotHubHostName;
            _gatewayHostName = gatewayHostName;
            _transportSettings = TransportHelper.GetSettings(certPath);
            _sasToken = sasToken;
        }

        private EdgeClient(string iotHubHostName, string gatewayHostName, string generationId, string edgeModuleId, string workloadUri) {
            _iotHubHostName = iotHubHostName;
            _gatewayHostName = gatewayHostName;
            _transportSettings = TransportHelper.GetSettings();
            _generationId = generationId;
            _edgeModuleId = edgeModuleId;
            _workloadUri = workloadUri;
            _useWorkloadApi = true;
        }

        public static EdgeClient CreateFromEnvironment() {
            EdgeClient client;
            var certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            var connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");
            if (string.IsNullOrEmpty(certPath) || string.IsNullOrEmpty(connectionString)) {
                var iotHubHostName = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");
                var gatewayHostName = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
                var generationId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEGENERATIONID");
                var edgeModuleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
                var workloadUri = Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI");
                if (string.IsNullOrEmpty(iotHubHostName) || string.IsNullOrEmpty(gatewayHostName) || 
                    string.IsNullOrEmpty(generationId) || string.IsNullOrEmpty(edgeModuleId) || 
                    string.IsNullOrEmpty(workloadUri)) {
                    throw new Exception("Configuration error - check environment variables");
                }
                client = new EdgeClient(iotHubHostName, gatewayHostName, generationId, edgeModuleId, workloadUri);
            } else {
                var csb = new DbConnectionStringBuilder {
                    ConnectionString = connectionString
                };
                if (!csb.ContainsKey("HostName") || !csb.ContainsKey("GatewayHostName") || !csb.ContainsKey("DeviceId") || 
                    !csb.ContainsKey("ModuleId") || !csb.ContainsKey("SharedAccessKey")) {
                    throw new Exception("Configuration error - check EdgeHubConnectionString variable");
                }
                var iotHubHostName = csb["HostName"].ToString(); 
                var gatewayHostName = csb["GatewayHostName"].ToString();
                // var edgeDeviceId = csb["DeviceId"].ToString();
                // var edgeModuleId = csb["ModuleId"].ToString();
                var sasToken = csb["SharedAccessKey"].ToString();
                client = new EdgeClient(iotHubHostName, gatewayHostName, certPath, sasToken);
            }
            return client;
        }

        public async Task<ModuleClient> CreateModuleClientAsync() {
            return await ModuleClient.CreateFromEnvironmentAsync(_transportSettings); 
            // ModuleClient.CreateFromConnectionString(_connectionString, _transportSettings);
        }

        public async Task<DeviceClient> CreateDeviceClientAsync(string leafDeviceId) {
            var key = await SignAsync(leafDeviceId);
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(leafDeviceId, key);
            return DeviceClient.Create(_iotHubHostName, _gatewayHostName, authMethod, _transportSettings, new ClientOptions { ModelId = ModelId });
        }

        private async Task<string> SignAsync(string payload) {
            if (_useWorkloadApi) {
                return await WorkloadApiClient.SignAsync(_workloadUri, _edgeModuleId, _generationId, payload);
            }
            using var hmac = new HMACSHA256(Convert.FromBase64String(_sasToken));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

    }

}