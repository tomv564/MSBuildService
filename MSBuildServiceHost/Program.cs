using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using StreamJsonRpc;
using Nerdbank.Streams;


namespace MSBuildServiceHost
{
    class LogMessage
    {
        public string Message { get; set; }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            Task.WaitAll(RespondToRpcRequestsAsync(FullDuplexStream.Splice(Console.OpenStandardInput(), Console.OpenStandardOutput()), 0));
            return 0;
        }

        private static async Task RespondToRpcRequestsAsync(Stream stream, int clientId)
        {
            await Console.Error.WriteLineAsync($"Connection request #{clientId} received. Spinning off an async Task to cater to requests.");
            var service = new MSBuildService.MSBuildService();

            var jsonRpc = JsonRpc.Attach(stream, service);
            //jsonRpc.TraceSource = new System.Diagnostics.TraceSource("jsonrpc");
            service.SetLogger(message =>
            {
                jsonRpc.NotifyAsync("LogMessage", new LogMessage { Message = message });
            });

            await Console.Error.WriteLineAsync($"JSON-RPC listener attached to #{clientId}. Waiting for requests...");
            await jsonRpc.Completion;
            await Console.Error.WriteLineAsync($"Connection #{clientId} terminated.");
        }
    }
}
