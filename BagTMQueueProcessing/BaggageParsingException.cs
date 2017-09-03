using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMQueueProcessing
{
    public class BaggageParsingException : Exception
    {
        public BaggageParsingException (String exceptionMessage) : base(exceptionMessage)
        {
        }
    }
}
