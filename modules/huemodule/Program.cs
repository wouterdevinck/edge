using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace huemodule {

    internal class Program {

        // TODO Make this configurable as environment variables
        private const string HueToken = "xxxx";

        static void Main() {

            // Read settings from environment
            var edge = EdgeClient.CreateFromEnvironment();

            // Initialize the identity translation logic
            var itm = new HueIdentityTranslator(HueToken, edge);
            itm.ConnectAsync().Wait();

            // Set up a cts to keep the module running until stopped
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            // ReSharper disable once MethodSupportsCancellation
            WhenCancelled(cts.Token).Wait();

        }

        private static Task WhenCancelled(CancellationToken cancellationToken) {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s)?.SetResult(true), tcs);
            return tcs.Task;
        }

    }

}