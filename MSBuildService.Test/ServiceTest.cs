using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace MSBuildService.Test
{
    [TestClass]
    public class ServiceTest
    {
        [TestMethod]
        public void CanCreate()
        {

            var service = new MSBuildService();

        }

        [TestMethod]
        public void CanLoadSolution()
        {
            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Projects\MSBuildService\MSBuildService.sln"));
        }

        [TestMethod]
        public void CanLoadSolutionMissingFile()
        {
            var service = new MSBuildService();

            Assert.IsFalse(service.LoadSolution(""));
        }

        [TestMethod]
        public void CannotBuildUnknownProject()
        {
            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Projects\MSBuildService\MSBuildService.sln"));

            Assert.IsFalse(service.BuildProject("", "Debug"));
        }

        [TestMethod()]
        public void CanBuildProject()
        {
            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Projects\MSBuildService\MSBuildService.sln"));

            Assert.IsTrue(service.BuildProject("MSBuildService", "Debug"));
        }



        [TestMethod]
        public void CanBuildSimpleCppSolution()
        {

            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Downloads\d3d12-hello-world-samples-win32\src\D3D12HelloWorld.sln"));

            Assert.IsTrue(service.BuildSolution("Debug"));
            // 
        }



        [TestMethod]
        public void CanBuildSimpleCppProject()
        {

            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Downloads\d3d12-hello-world-samples-win32\src\D3D12HelloWorld.sln"));

            Assert.IsTrue(service.BuildProject("D3D12HelloTriangle", "Debug"));
            // 
        }


        [TestMethod()]
        public void CanBuildCppFileInProject()
        {
            var service = new MSBuildService();

            //service.SetLogger(message => Debug.WriteLine(message));

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Downloads\d3d12-hello-world-samples-win32\src\D3D12HelloWorld.sln"));

            Assert.IsTrue(service.BuildFile("D3D12HelloTriangle", @"C:\Users\Tom\Downloads\d3d12-hello-world-samples-win32\src\HelloTriangle\D3D12HelloTriangle.cpp", "Debug"));
        }

        //[TestMethod]
        //public void CanBuildCppProject()
        //{
        //    var service = new MSBuildService();

        //    Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Projects\cppgamedev\build\cppgamedev.sln"));

        //    Assert.IsTrue(service.BuildProject("game", "RelWithDebInfo"));

        //}

        [TestMethod]
        public void CanFindProjectForCSFile()
        {
            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Projects\MSBuildService\MSBuildService.sln"));

            var result = service.FindProjectForFile(@"C:\Users\Tom\Projects\MSBuildService\MSBuildService\MSBuildService.cs");

            Assert.IsNotNull(result);

            Assert.AreEqual(result.ProjectName, "MSBuildService");
        }


        [TestMethod]
        public void CanFindProjectForCPPFile()
        {
            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Downloads\d3d12-hello-world-samples-win32\src\D3D12HelloWorld.sln"));

            var result = service.FindProjectForFile(@"C:\Users\Tom\Downloads\d3d12-hello-world-samples-win32\src\HelloTriangle\D3D12HelloTriangle.cpp");

            Assert.IsNotNull(result);

            Assert.AreEqual(result.ProjectName, "D3D12HelloTriangle");
        }


        [TestMethod]
        public void CanFindProjectForHFile()
        {
            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Downloads\d3d12-hello-world-samples-win32\src\D3D12HelloWorld.sln"));

            var result = service.FindProjectForFile(@"C:\Users\Tom\Downloads\d3d12-hello-world-samples-win32\src\HelloTriangle\D3D12HelloTriangle.h");

            Assert.IsNotNull(result);

            Assert.AreEqual(result.ProjectName, "D3D12HelloTriangle");
        }

        [TestMethod]
        public void HandlesNotFindingProjectForFile()
        {
            var service = new MSBuildService();

            Assert.IsTrue(service.LoadSolution(@"C:\Users\Tom\Projects\MSBuildService\MSBuildService.sln"));

            var result = service.FindProjectForFile(@"");

            Assert.IsNull(result);

        }
    }
}