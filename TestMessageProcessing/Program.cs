using System;
using System.Configuration;
using BagTMEngineProcessing;
using BagTMQueueProcessing;
using System.Diagnostics;
using BagTMCommon;

namespace TestMessageProcessing
{
    class Program
    {
        
        static void Main(string[] args)
        {
            try
            {
                BagTMLog.LogDebug("BagTM Command Line Start", args);

                BagTMLog.LogDebug("BagTM Queue Processing Debug Starting", null);

                IExecutionQueueProcessing queueProcess = new ExecutionDataflowQueueProcessing();

                queueProcess.QueueProcessingDebug();

                BagTMLog.LogDebug("BagTM Queue Processing Debug Ended", null);

                BagTMLog.LogDebug("BagTM Engine Processing Debug Starting", null);

                IExecutionEngineProcessing engineProcess = new ExecutionDataflowEngineProcessing();

                engineProcess.EngineProcessingDebug();

                BagTMLog.LogDebug("BagTM Command Line Debug Ends", null);

                BagTMLog.LogDebug("BagTM Command Line Ending", null);

            } 
            catch (ConfigurationErrorsException cee)
            {
                BagTMLog.LogError("BagTM Command Line Ends", args, cee);
            }
            
        }
    }
}
