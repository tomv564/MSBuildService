using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nerdbank.Streams;
using StreamJsonRpc;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MSBuildService.IntegrationTest
{

    public class LogMessage
    {
        public string Message { get; set; }
    }

    [TestClass]
    public class IntegrationTest
    {

        //[TestInitialize]
        //public void Setup()
        //{
        //    var psi = new ProcessStartInfo(FindPathToServer(), "stdio");
        //    psi.RedirectStandardInput = true;
        //    psi.RedirectStandardOutput = true;
        //    var process = Process.Start(psi);
        //    var stdioStream = FullDuplexStream.Splice(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
        //    await ActAsRpcClientAsync(stdioStream);
        //}


        public delegate void LogMessageHandler(LogMessage message);

        private void OnLogMessage(LogMessage message)
        {
            Console.WriteLine(message.Message);
        }


        [TestMethod]
        public async Task FullTest()
        {
            var serverPath = FindPathToServer();
            if (!File.Exists(serverPath))
            {
                Console.WriteLine(@"Server not found, looked at: {serverPath}");
                throw new FileNotFoundException(serverPath);
            }
            var psi = new ProcessStartInfo(serverPath, "stdio");
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            var process = Process.Start(psi);
            var stdioStream = FullDuplexStream.Splice(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
            //await ActAsRpcClientAsync(stdioStream);

            Console.WriteLine("Connected. Sending request...");
            //using var jsonRpc = JsonRpc.Attach(stream);
            var jsonRpc = new JsonRpc(stdioStream);

            LogMessageHandler handler = this.OnLogMessage;
            jsonRpc.AddLocalRpcMethod("LogMessage", handler);
            
            jsonRpc.StartListening();


            //jsonRpc.
            bool result = await jsonRpc.InvokeAsync<bool>("LoadSolution", @"C:\Users\Tom\Projects\MSBuildService\MSBuildService.sln");

            Assert.IsTrue(result);

            var info = await jsonRpc.InvokeAsync<ProjectFileInfo>("FindProjectForFile", @"C:\Users\Tom\Projects\MSBuildService\MSBuildService\MSBuildService.cs");

            Assert.IsNotNull(info);

            Assert.AreEqual(info.ProjectName, "MSBuildService");

            process.Close();
        }

        private static async Task ActAsRpcClientAsync(Stream stream)
        {
            Console.WriteLine("Connected. Sending request...");
            //using var jsonRpc = JsonRpc.Attach(stream);
            var jsonRpc = JsonRpc.Attach(stream);

            bool result = await jsonRpc.InvokeAsync<bool>("LoadSolution", "lol.sln");

            Assert.IsFalse(result);
            
            //Console.WriteLine($"3 + 5 = {sum}");
        }

        private static string FindPathToServer()
        {
#if DEBUG
            const string Config = "Debug";
#else
        const string Config = "Release";
#endif
            return Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                $@"..\..\..\MSBuildServiceHost\bin\{Config}\MSBuildServiceHost.exe");
        }
    }
}
