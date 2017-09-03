using System;

namespace BagTMQueueProcessing
{
    class QueueProcessingException : Exception
    {
        public QueueProcessingException(String exceptionMessage)
        {
            new Exception(exceptionMessage);
        }
    }
}
