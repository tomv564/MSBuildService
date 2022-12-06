namespace MSBuildService
{

    public class ProjectFileInfo
    {
        public string ProjectName { get; set; }

    }

    public interface IMSBuildService
    {
        bool BuildFile(string projectName, string filePath, string configurationName);

        bool BuildProject(string projectName, string configurationName);
        bool BuildSolution(string configurationName);
        ProjectFileInfo FindProjectForFile(string filePath);
        bool LoadSolution(string filePath);
    }
}