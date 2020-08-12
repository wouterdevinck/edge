using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace huemodule {

    internal class TransportHelper {

        public static ITransportSettings[] GetSettings(string certPath) {
            // Build transport settings for module and child device connections
            var trustedCertificate = new X509Certificate2(X509Certificate.CreateFromCertFile(certPath));
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                    // Custom validation logic based on: 
                    // https://github.com/Azure/azure-iot-sdk-csharp/blob/2020-05-06/iothub/device/src/Edge/CustomCertificateValidator.cs#L82
                    // Alternatively, the certificate can be added to the trust store, like done here: 
                    // https://github.com/Azure/iotedge/blob/1.0.9.3/samples/dotnet/EdgeDownstreamDevice/Program.cs#L74
                    var terminatingErrors = sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors;
                    if (terminatingErrors != SslPolicyErrors.None) {
                        Console.WriteLine("Discovered SSL session errors: {0}", terminatingErrors);
                        return false;
                    }
                    chain.ChainPolicy.ExtraStore.Add(trustedCertificate);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    if (!chain.Build(new X509Certificate2(certificate))) {
                        Console.WriteLine("Unable to build the chain using the expected root certificate.");
                        return false;
                    }
                    var actualRoot = chain.ChainElements[^1].Certificate;
                    if (!trustedCertificate.Equals(actualRoot)) {
                        Console.WriteLine("The certificate chain was not signed by the trusted root certificate.");
                        return false;
                    }
                    return true;
                }
            };
            return new ITransportSettings[] { mqttSetting };
        }

        public static ITransportSettings[] GetSettings() {
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            return new ITransportSettings[] { mqttSetting };
        }

    }

}
