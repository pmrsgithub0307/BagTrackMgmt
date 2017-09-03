using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBM.WMQ;
using System.Collections;

namespace BagTMCommon
{
    /// <summary>
    /// Author: Reza Servia
    /// Name: MQTest.cs
    /// Description:
    /// MQ Manager Class
    /// Put/Get Messages To/From Local Queue 
    /// </summary>
    public class MQSrMessageQueueII
    {
        MQQueueManager queueManager;
        MQQueue queue;
        MQMessage queueMessage;
        MQPutMessageOptions queuePutMessageOptions;
        MQGetMessageOptions queueGetMessageOptions;
        static string SendQueueName;
        static string ReceiveQueueName;
        static string QueueManagerName;
        static string ChannelInfo;
        string channelName;
        string transportType;
        string connectionName;
        string message;
        private Hashtable queueProperties;

        public MQSrMessageQueueII()
        {
            //Initialization
            
            QueueManagerName = "FI.CADS";
            ReceiveQueueName = "FI.WM.TST.BAGGAGEMONITOR";
            ChannelInfo = "SYSTEM.DEF.SVRCONN/TCP/ICEKOPAS18/1414/mqm";

            ConnectMQ(QueueManagerName, ChannelInfo);
            
        }
        /// <summary>
        /// Connect to MQ Server
        /// </summary>
        /// <param name="strQueueManagerName">Queue Manager Name</param>
        /// <param name="strChannelInfo">Channel Information</param>
        /// <returns></returns>
        public string ConnectMQ(string strQueueManagerName, string strChannelInfo)
        {
            QueueManagerName = strQueueManagerName;
            ChannelInfo = strChannelInfo;
            char[] separator = { '/' };
            string[] ChannelParams;
            ChannelParams = ChannelInfo.Split(separator);
            channelName = ChannelParams[0];
            transportType = ChannelParams[1];
            connectionName = ChannelParams[2];
            string strReturn = "";
            try
            {
                queueProperties = new Hashtable();
                queueProperties[MQC.HOST_NAME_PROPERTY] = ChannelParams[2];
                queueProperties[MQC.PORT_PROPERTY] = ChannelParams[3];
                queueProperties[MQC.CHANNEL_PROPERTY] = ChannelParams[0];
                //queueProperties[MQC.USER_ID_PROPERTY] = "mqm";
                //queueProperties[MQC.PASSWORD_PROPERTY] = "mqm";
                queueProperties[MQC.CCSID_PROPERTY] = MQC.CODESET_819;
                queueProperties[MQC.TRANSPORT_PROPERTY] = MQC.TRANSPORT_MQSERIES;

                queueManager = new MQQueueManager(QueueManagerName, queueProperties);
                strReturn = "Connected Successfully";
            }
            catch (MQException exp)
            {
                strReturn = "Exception: " + exp.Message;
            }
            catch (Exception exp)
            {
                strReturn = "Exception: " + exp.Message;
            }
            return strReturn;
        }
        /// <summary>
        /// Write Message to Local Queue
        /// </summary>
        /// <param name="strInputMsg">Text Message</param>
        /// <param name="strqueueName">Queue Name</param>
        /// <returns></returns>
        public string WriteLocalQMsg(string strInputMsg, string strQueueName)
        {
            string strReturn = "";
            SendQueueName = strQueueName;
            try
            {
                queue = queueManager.AccessQueue(SendQueueName,
                MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING);
                message = strInputMsg;
                queueMessage = new MQMessage();
                queueMessage.WriteString(message);
                queueMessage.Format = MQC.MQFMT_STRING;
                queuePutMessageOptions = new MQPutMessageOptions();
                queue.Put(queueMessage, queuePutMessageOptions);
                strReturn = "Message sent to the queue successfully";
            }
            catch (MQException MQexp)
            {
                strReturn = "Exception: " + MQexp.Message;
            }
            catch (Exception exp)
            {
                strReturn = "Exception: " + exp.Message;
            }
            return strReturn;
        }
        /// <summary>
        /// Write Message to Local Queue
        /// </summary>
        /// <param name="strInputMsg">Text Message</param>
        /// <returns></returns>
        public string WriteLocalQMsg(string strInputMsg)
        {
            string strReturn = "";
            try
            {
                queue = queueManager.AccessQueue(SendQueueName,
                MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING);
                message = strInputMsg;
                queueMessage = new MQMessage();
                queueMessage.WriteString(message);
                queueMessage.Format = MQC.MQFMT_STRING;
                queuePutMessageOptions = new MQPutMessageOptions();
                queue.Put(queueMessage, queuePutMessageOptions);
                strReturn = "Message sent to the queue successfully";
            }
            catch (MQException MQexp)
            {
                strReturn = "Exception: " + MQexp.Message;
            }
            catch (Exception exp)
            {
                strReturn = "Exception: " + exp.Message;
            }
            return strReturn;
        }
        /// <summary>
        /// Read Message from Local Queue
        /// </summary>
        /// <param name="strqueueName">Queue Name</param>
        /// <returns>Text Message</returns>
        public string ReadLocalQMsg(string strQueueName)
        {
            string strReturn = "";
            ReceiveQueueName = strQueueName;
            try
            {
                queue = queueManager.AccessQueue(ReceiveQueueName,
                MQC.MQOO_INPUT_AS_Q_DEF + MQC.MQOO_FAIL_IF_QUIESCING);
                queueMessage = new MQMessage();
                queueMessage.Format = MQC.MQFMT_STRING;
                queueGetMessageOptions = new MQGetMessageOptions();
                queue.Get(queueMessage, queueGetMessageOptions);
                strReturn = queueMessage.ReadString(queueMessage.MessageLength);
            }
            catch (MQException MQexp)
            {
                strReturn = "Exception: " + MQexp.Message;
            }
            catch (Exception exp)
            {
                strReturn = "Exception: " + exp.Message;
            }
            return strReturn;
        }
    }
}
