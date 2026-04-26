using GroundAgent.BuildDefinitions;
using GroundAgent.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GroundAgent.Tasks
{
    public interface ITaskBuilder
    {
        Task<IEnumerable<TaskStep>> Build(
            Build build,
            IAppConfiguration configuration);
    }
}
