using System.Collections.Generic;

namespace GroundAgent.Report
{
    public interface IBuildReporter
    {
        /// <summary>
        /// Report build results.
        /// </summary>
        /// <param name="outputStepReport"></param>
        void ReportBuildResults(List<StepReport> outputStepReport);
    }
}
