using Microsoft.Build;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System.Diagnostics;
using Microsoft.Build.Locator;
using Microsoft.Build.Framework;
using System.Linq;
using System;
using System.IO;
using Microsoft.Build.Logging;
using Microsoft.Build.Execution;
using System.Collections.Generic;

namespace MSBuildService
{

    public delegate void LoggingDelegate(string message);

    public class MSBuildService : IMSBuildService
    {

        private SolutionFile solutionFile;
        private string solutionPath;

        private LoggingDelegate _loggingDelegate;
        public void SetLogger(LoggingDelegate loggingFunc)
        {
            this._loggingDelegate = loggingFunc;
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);
            if (this._loggingDelegate != null)
                this._loggingDelegate(message);
        }

        static MSBuildService()
        {

            // NOTE: needed to target .NET 4.7.1 to detect msbuild in visual studio installs.
            var query = VisualStudioInstanceQueryOptions.Default;

            var instances = MSBuildLocator.QueryVisualStudioInstances(query);
            
            //foreach (var i in instances)
            //    Log($"{i.Name}: ${i.MSBuildPath}");
            
            var instance = instances.FirstOrDefault();
            if (instance != null)
            {
                Debug.WriteLine($"Found instance {instance.Name} ({instance.MSBuildPath})");
                MSBuildLocator.RegisterInstance(instance);
            }
            else
                throw new ApplicationException("no msbuild instance found!");

            // If getting this error, replace binding redirects in app.config:

            // "Could not load file or assembly 'System.Memory, Version=4.0.1.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The system cannot find the file specified."
            // C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\System.Memory.dll is 4.0.1.1, token  cc7b13ffcd2ddd51

            /*
             * Can be System.Memory 4.0.1.2 also.
             * 
             * 
 <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="System.Collections.Immutable" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-5.0.0.0" newVersion="5.0.0.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Memory" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.0.1.1" newVersion="4.0.1.1" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Runtime.CompilerServices.Unsafe" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-5.0.0.0" newVersion="5.0.0.0" />
      </dependentAssembly>
    </assemblyBinding>
             * 
             */


        }

        public bool LoadSolution(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            solutionPath = filePath;

            solutionFile = SolutionFile.Parse(filePath);

            Log($"Loaded {filePath}");
            this.Log(string.Format("Default configuration and platform are {0} | {1}", solutionFile.GetDefaultConfigurationName(), solutionFile.GetDefaultPlatformName()));

            foreach (var item in solutionFile.ProjectsInOrder)
            {
                if (item.ProjectType == SolutionProjectType.SolutionFolder)
                    continue;
               
                Project project = ProjectCollection.GlobalProjectCollection.LoadProject(item.AbsolutePath);
                Log($"Loaded project {project.FullPath}");
            }

            return true;
        }

        public ProjectFileInfo FindProjectForFile(string filePath)
        {
            if (solutionFile == null)
                return null;

            filePath = filePath.Replace("/", "\\");

            //var matchingInclude = options.ProjectFileMatchFullPath ? filePath : Path.GetFileName(filePath);
            //var matchingInclude = filePath;
            var fileName = Path.GetFileName(filePath);
            //var directory = Path.GetDirectoryName(filePath);

            var found = ProjectCollection.GlobalProjectCollection.LoadedProjects.FirstOrDefault(project =>
            {
                // c#
                if (project.FullPath.EndsWith("csproj"))
                {
                    return project.Items.Where(i => i.ItemType == "Compile").Any(i =>
                    {
                        return i.EvaluatedInclude == fileName;
                    });
                }
                else
                {
                    return project.Items.Where(i => i.ItemType == "ClCompile" || i.ItemType == "ClInclude").Any(i =>
                    {
                        // TODO: Can we move this check up to per-project or even per-solution?
                        if (Path.IsPathRooted(i.EvaluatedInclude))
                            return i.EvaluatedInclude == filePath;
                        else
                            return i.EvaluatedInclude == fileName;
                    });
                }

            });

            if (found != null)
            {
                return new ProjectFileInfo { ProjectName = found.GetPropertyValue("MSBuildProjectName") };
            }

            return null;
        }

        public bool BuildSolution(string configurationName)
        {
            if (solutionFile == null)
                return false;

            BuildManager manager = BuildManager.DefaultBuildManager;
            manager.ResetCaches();

            var parameters = new BuildParameters(ProjectCollection.GlobalProjectCollection);
            var logger = new ConsoleLogger(LoggerVerbosity.Minimal, this.ForwardMessage, this.SetConsoleColor, this.ResetConsoleColor);
            logger.ShowSummary = true;
            parameters.Loggers = new List<ILogger> { logger };

            var globalProperties = new Dictionary<String, String>();
            globalProperties.Add("Configuration", configurationName);
            globalProperties.Add("Platform", "x64");

            BuildRequestData request = new BuildRequestData(solutionPath, globalProperties, null, new String[] { "Build" }, null);

            var result = manager.Build(parameters, request);
            
            return result.OverallResult == BuildResultCode.Success;
        }

        public bool BuildProject(string projectName, string configurationName)
        {
            if (solutionFile == null)
                return false;


            var project = solutionFile.ProjectsInOrder.FirstOrDefault(p => p.ProjectName == projectName);

            if (project == null)
                return false;

            var buildProjects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(project.AbsolutePath);

            var buildProject = buildProjects.FirstOrDefault();
            if (buildProject == null)
            {
                Log($"Could not find loaded project for known project {projectName}");
                return false;
            }

            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Configuration", configurationName);
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Platform", "x64");
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("VCToolsInstallDir", @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.30.30705\");
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("WindowsSDKDir", @"C:\Program Files (x86)\Windows Kits\10\");

            ConsoleLogger logger = new ConsoleLogger(LoggerVerbosity.Minimal, this.ForwardMessage, this.SetConsoleColor, this.ResetConsoleColor);
            logger.ShowSummary = true;


            //Log(string.Format("building {0}, toolsversion {1}", projectName, buildProject.ToolsVersion));
            bool result = buildProject.Build(logger);
            //Log("done");

            return result;
        }


        public bool BuildFile(string projectName, string filePath, string configurationName)
        {
            if (solutionFile == null)
                return false;


            var project = solutionFile.ProjectsInOrder.FirstOrDefault(p => p.ProjectName == projectName);

            if (project == null)
                return false;

            var buildProjects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(project.AbsolutePath);

            var buildProject = buildProjects.FirstOrDefault();
            if (buildProject == null)
            {
                Log($"Could not find loaded project for known project {projectName}");
                return false;
            }

            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Configuration", configurationName);
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Platform", "x64");
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("VCToolsInstallDir", @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.30.30705\");
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("WindowsSDKDir", @"C:\Program Files (x86)\Windows Kits\10\");

           
            ConsoleLogger logger = new ConsoleLogger(LoggerVerbosity.Minimal, this.ForwardMessage, this.SetConsoleColor, this.ResetConsoleColor);
            logger.ShowSummary = true;

            //Log(string.Format("building {0}, toolsversion {1}", projectName, buildProject.ToolsVersion));
            var loggers = new System.Collections.Generic.List<ILogger>() { logger };
            var fileName = Path.GetFileName(filePath);
            buildProject.SetProperty("SelectedFiles", fileName);
            bool result = buildProject.Build("ClCompile", loggers); ///p:SelectedFiles=

            //Log("done");

            return result;
        }


        private void SetConsoleColor(ConsoleColor color)
        {
        }

        private void ResetConsoleColor()
        {
        }
        private void ForwardMessage(string message)
        {
            Debug.Write(message);
            if (this._loggingDelegate != null)
                this._loggingDelegate(message);
        }
    }

}