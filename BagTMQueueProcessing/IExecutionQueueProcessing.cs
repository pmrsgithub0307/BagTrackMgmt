using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMQueueProcessing
{
    public interface IExecutionQueueProcessing
    {
        /// <summary>
        /// Task creation for queue processing events
        /// </summary>
        void QueueProcessing();

        /// <summary>
        /// Task creation for queue processing events in debug from VS
        /// </summary>
        void QueueProcessingDebug();

    }
}
