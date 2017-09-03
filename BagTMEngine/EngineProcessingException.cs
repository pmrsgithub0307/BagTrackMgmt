using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMEngineProcessing
{
    class EngineProcessingException : Exception
    {
        public EngineProcessingException(String exceptionMessage) : base(exceptionMessage)
        {
        }
    }
}
