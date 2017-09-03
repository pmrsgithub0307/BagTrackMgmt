using NLog;
using System;
using System.Diagnostics;
using System.Reflection;

namespace BagTMCommon
{
    public class BagTMLog
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void LogDebug(String message, Object messageObject)
        {
            logger.Log(LogLevel.Debug,
                String.Format("{0} : {1}", message, LogObject(messageObject)));
        }

        public static void LogInfo(String message, Object messageObject)
        {
            logger.Log(LogLevel.Info, String.Format("{0}", message));
        }

        public static String LogError(String message, Object messageObject, Exception e)
        {
            Exception innerError = e.InnerException;
            String erroMsg = e.Message;
            while (innerError != null)
            {
                erroMsg = String.Format("{0} : {1}", erroMsg, innerError.Message);
                innerError = innerError.InnerException;
            } 

            logger.Log(LogLevel.Error,
                String.Format("{0} {1} {2} : Exception {3} : {4}", e.TargetSite.DeclaringType.FullName, e.TargetSite.Name, erroMsg, LogObject(messageObject), e.Message));
            logger.Log(LogLevel.Debug,
                String.Format("{0} {1} {2} : Exception {3} : {4}", e.TargetSite.DeclaringType.FullName, e.TargetSite.Name, erroMsg, LogObject(messageObject), e.Message));
            return String.Format("{0} {1} {2} : Exception {3} : {4}", e.TargetSite.DeclaringType.FullName, e.TargetSite.Name, erroMsg, LogObject(messageObject), e.Message);
        }

        public static void LogFatal(String message, Object messageObject)
        {
            logger.Log(LogLevel.Fatal,
                String.Format("{0} : {1}", message, LogObject(messageObject)));
        }

        private static String LogObject(Object messageObject)
        {
            if (messageObject == null) return "";
            else if (messageObject.GetType().Name.Equals("OSUSR_UUK_BAGINTEG")
                    || messageObject.GetType().Name.Equals("OSUSR_UUK_BAGMSGS")
                    || messageObject.GetType().Name.Equals("OSUSR_UUK_BAGTIMESREFERENCE")
                    || messageObject.GetType().Name.Equals("OSUSR_UUK_EQUIPTYPE")
                    || messageObject.GetType().Name.Equals("OSUSR_UUK_FLT_INFO")
                    || messageObject.GetType().Name.Equals("OSUSR_UUK_H2H")
                    || messageObject.GetType().Name.Equals("OSUSR_UUK_RULES")
                    || messageObject.GetType().Name.Equals("OSUSR_UUK_TAXITIMES"))
            {
                 MethodInfo methodInfo;
                 object[] parameters = new object[0];
                 String messageString = "{";
                 foreach (PropertyInfo propertyInfo in messageObject.GetType().GetProperties())
                 {
                    methodInfo = messageObject.GetType().GetMethod("get_" + propertyInfo.Name);
                    messageString += String.Format("{0} : {1}", propertyInfo.Name, methodInfo.Invoke(messageObject, parameters));
                 }
                messageString += "}";
                return messageString;
            }
            else if (messageObject.GetType().Name.Equals("PTMTTYTable")
                    || messageObject.GetType().Name.Equals("BaggageTTYTable"))
            {
                MethodInfo methodInfo;
                object[] parameters = new object[0];
                String messageString = "{";
                foreach (PropertyInfo propertyInfo in messageObject.GetType().BaseType.GetProperties())
                {
                    methodInfo = messageObject.GetType().BaseType.GetMethod("get_" + propertyInfo.Name);
                    messageString += String.Format("{0} : {1}", propertyInfo.Name, methodInfo.Invoke(messageObject, parameters));
                }
                messageString += "}";
                return messageString;
            }
            else if (messageObject.GetType().Name.Equals("ExecutionDataflowQueueProcessing")
                 || messageObject.GetType().Name.Equals("BaggageMessageParsing")
                 || messageObject.GetType().Name.Equals("ExecutionDataflowEngineProcessing")
                 || messageObject.GetType().Name.Equals("BagIntergityTransformation"))

            {
                StackTrace stackTrace = new StackTrace();
                return stackTrace.GetFrame(2).GetMethod().Name;
            }
            else return messageObject.ToString();
        }

    }
}
