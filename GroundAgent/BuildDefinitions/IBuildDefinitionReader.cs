namespace GroundAgent.BuildDefinitions
{
    public interface IBuildDefinitionReader
    {
        Build GetBuild(string buildYamlPath);
    }
}
