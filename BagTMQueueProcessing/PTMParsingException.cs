using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMQueueProcessing
{
    public class PTMParsingException : Exception
    {
        public PTMParsingException (String exceptionMessage)
        {
            new Exception(exceptionMessage);
        }
    }
}
