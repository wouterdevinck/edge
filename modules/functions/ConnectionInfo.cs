using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace functions {

    public static class ConnectionInfo {

        [FunctionName("ConnectionInfo")]
        public static SignalRConnectionInfo Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "negotiate")] HttpRequest req, [SignalRConnectionInfo(HubName = "notifications")] SignalRConnectionInfo connectionInfo) {
            return connectionInfo;
        }

    }

}
