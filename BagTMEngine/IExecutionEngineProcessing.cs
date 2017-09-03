using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMEngineProcessing
{
    public interface IExecutionEngineProcessing
    {
        /// <summary>
        /// Task creation for engine processing events
        /// </summary>
        void EngineProcessing();

        /// <summary>
        /// Task creation for engine processing events in debug from VS
        /// </summary>
        void EngineProcessingDebug();

    }
}
