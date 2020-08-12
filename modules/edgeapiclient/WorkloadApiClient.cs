using System;
using System.Text;
using System.Threading.Tasks;
using iotedgeapiclient.helper;

namespace iotedgeapiclient {

    public class WorkloadApiClient {

        // See https://github.com/Azure-Samples/azure-iot-edge-identity-translation-lite/blob/1d77b79944/src/edge/modules/IdentityTranslationLite/Program.cs#L93
        // Possible to use this instead? https://github.com/Azure/iotedge/blob/master/edge-util/src/Microsoft.Azure.Devices.Edge.Util/edged/WorkloadClient.cs

        public static async Task<string> SignAsync(string worloadUriPath, string moduleId, string generationId, string payload) {
            const string workloadApiVersion = "2019-01-30";
            var workloadUri = new Uri(worloadUriPath);
            using var httpClient = HttpClientHelper.GetHttpClient(workloadUri);
            httpClient.BaseAddress = new Uri(HttpClientHelper.GetBaseUrl(workloadUri));
            var workloadClient = new WorkloadClient(httpClient);
            var signRequest = new SignRequest {
                KeyId = "primary", // or "secondary"
                Algo = SignRequestAlgo.HMACSHA256,
                Data = Encoding.UTF8.GetBytes(payload)
            };
            var signResponse = await workloadClient.SignAsync(workloadApiVersion, moduleId, generationId, signRequest);
            var signedPayload = Convert.ToBase64String(signResponse.Digest);
            return signedPayload;
        }

    }

}
