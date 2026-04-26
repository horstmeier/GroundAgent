using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GroundAgent.BuildDefinitions
{
    public class BuildDefinitionReader : IBuildDefinitionReader
    {
        public Build GetBuild(string buildYamlPath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var build = File.ReadAllText(buildYamlPath);
            var input = new StringReader(build);

            return deserializer.Deserialize<Build>(input);
        }
    }
}
