using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Messaging;
using System.Configuration;
using BagTMDBLibrary;
using BagTMCommon;

namespace BagTMQueueProcessing
{
    public class ExecutionDataflowQueueProcessing : IExecutionQueueProcessing
    {
        /// <summary>
        /// Number of parallel task for queue processing
        /// </summary>
        private int parallelismCounter;

        /// <summary>
        /// Hub information
        /// </summary>
        private String hub;

        /// <summary>
        /// Get the application configuration file.
        /// </summary>
        Configuration config;

        /// <summary>
        /// TTY baggage tty queue replica of Amadeus queue
        /// </summary>
        private MessageQueue queueIn;
        private MessageQueue queueInError;
        private MessageQueue queueInSucess;

        private const int QUEUESUCESS = 1;
        private const int QUEUEERROR = 2;
        private const int QUEUEENGINE = 3;
        private const int QUEUEIN = 4;

        /// <summary>
        /// TTY baggage tty queue replica of Amadeus queue
        /// </summary>
        private MessageQueue queueEngine;
        

        /// <summary>
        /// Timelimit to wait for new messages in the queue. Assumption that timer on service will pick after
        /// </summary>
        private TimeSpan timeSpan;

        /// <summary>
        /// Formater das mensagens MQ TTY
        /// </summary>
        private IMessageFormatter messageFormater;

        /// <summary>
        /// Controles action block paralellism
        /// </summary>
        private bool isProcessing = false;

        /// <summary>
        /// Control debug execution
        /// </summary>
        private String isDebug = "";


        /// <summary>
        /// Constructor to create logger and tasks and queue parametrization
        /// </summary>
        public ExecutionDataflowQueueProcessing ()
        {
            BagTMLog.LogDebug("BagTM Queue Processing Constructor", this);

            //Get the application configuration file.
            config =
                    ConfigurationManager.OpenExeConfiguration(
                        ConfigurationUserLevel.None) as Configuration;

            this.hub = this.GetAppSetting(config, "hub");

            String timeSpanParameter = this.GetAppSetting(config, "timeSpan");
            int timeSpanNumber = 0;
            if (!int.TryParse(timeSpanParameter, out timeSpanNumber))
                throw new QueueProcessingException("Parameter TimeSpan incorrectly configured");

            this.timeSpan = new TimeSpan((Int64)(timeSpanNumber * TimeSpan.TicksPerSecond));

            BagTMLog.LogDebug("BagTM Queue Processing Timespan " + timeSpanParameter, timeSpan);
            
            String parallelismCounterParameter = this.GetAppSetting(config, "parallelismCounter");
            if (!int.TryParse(parallelismCounterParameter, out this.parallelismCounter))
                throw new QueueProcessingException("Parameter ParallelismCounter incorrectly configured");

            BagTMLog.LogDebug("BagTM Queue Processing ParallelismCounter " + parallelismCounter, parallelismCounter);
            
            TTYMessagesSection ttyMsg = ConfigurationManager.GetSection("TTYMessagesSection") as TTYMessagesSection;

            BagTMLog.LogDebug(String.Format("BagTM Queue Processing TTY Parser Collection {0} {1}", 
                                ttyMsg.ttyCollection.GetNumberOfItems(),
                                ttyMsg.ttyCollection.Describe())
                            , this);

            try
            {
                this.messageFormater = new TTYMessageFormatter(ttyMsg.ttyCollection, new BaggageEntities(), hub);

                BagTMLog.LogDebug("BagTM Queue Processing MessageFormater", messageFormater);

                queueIn = this.CreateMessageQueue(QUEUEIN);
                queueInSucess = this.CreateMessageQueue(QUEUESUCESS);
                queueInError = this.CreateMessageQueue(QUEUEERROR);
                queueEngine = this.CreateMessageQueue(QUEUEENGINE);
                BagTMLog.LogDebug("BagTM Queue Processing QueueEngine", queueEngine);
                BagTMLog.LogDebug("BagTM Queue Processing Constructor Ending", this);
            } 
            catch (Exception e)
            {
                BagTMLog.LogError("BagTM Queue Processing Constructor Error ", this, e);
            }
            
        }

        /// <summary>
        /// Task creation for queue processing events
        /// </summary>
        public void QueueProcessing()
        {
            BagTMLog.LogDebug(
                    String.Format("BagTM Queue Processing Queue Process Start "), this);

            if (this.isProcessing)
            {
                BagTMLog.LogDebug(
                    String.Format("BagTM Queue Processing Queue Process Is Processing"), this);
                return;
            }

            BagTMLog.LogDebug(
                    String.Format("BagTM Queue Processing Task Process Message Define Dataflow Block Number of Messages {0}", parallelismCounter)
                    , this);

            Action<int> queueProcess = n => {
                BagTMLog.LogDebug(
                    String.Format("BagTM Queue Processing Task Process Message {0}", n), this);

                QueueProcess(n);

                BagTMLog.LogDebug(
                    String.Format("BagTM Queue Processing Task Process Message End {0}", n), this);

            };

            var opts = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = parallelismCounter
            };

            var actionBlock = new ActionBlock<int>(queueProcess, opts);

            this.isProcessing = true;

            try
            {
                for (int i = actionBlock.InputCount; i < parallelismCounter; i++) actionBlock.Post(i);

                actionBlock.Complete();
                actionBlock.Completion.Wait();

                this.isProcessing = false;
            }
            catch (Exception e)
            {
                this.isProcessing = false;
                throw e;
            }
            finally
            {
                this.isProcessing = false;
            }
            BagTMLog.LogDebug(
                    String.Format("BagTM Queue Processing Queue Process End "), this);

        }

        public void QueueProcessingDebug()
        {
            isDebug = "_debug";

            queueIn = this.CreateMessageQueue(QUEUEIN);
            queueInSucess = this.CreateMessageQueue(QUEUESUCESS);
            queueInError = this.CreateMessageQueue(QUEUEERROR);
            queueEngine = this.CreateMessageQueue(QUEUEENGINE);

            QueueProcess(0);
        }

        public void QueueProcess(int n)
        {
            BagTMLog.LogDebug(
                    String.Format("BagTM Queue Debug Processing Queue Process Start"), this);

            object[] messages = null;
            try
            {
                messages = this.ReadQueue(n);
                Message messageEngine = new Message(messages[0]);
                if (messages[0].GetType().Equals(typeof(OSUSR_UUK_BAGMSGS)))
                {
                    messageEngine.Label = "Bag Msgs: " + ((OSUSR_UUK_BAGMSGS)messages[0]).ID;
                }
                else if (messages[0].GetType().Equals(typeof(OSUSR_UUK_PAXMSGS)))
                {
                    messageEngine.Label = "Pax Msgs: " + ((OSUSR_UUK_PAXMSGS)messages[0]).ID;
                }
                this.SendMessage(messageEngine, n, QUEUEENGINE);
                Message messageSucess = (Message)messages[1];
                if (messages[0].GetType().Equals(typeof(OSUSR_UUK_BAGMSGS)))
                {
                    messageSucess.Label = "Bag Msgs: " + ((OSUSR_UUK_BAGMSGS)messages[0]).ID;
                }
                else if (messages[0].GetType().Equals(typeof(OSUSR_UUK_PAXMSGS)))
                {
                    messageSucess.Label = "Pax Msgs: " + ((OSUSR_UUK_PAXMSGS)messages[0]).ID;
                }
                this.SendMessage(messageSucess, n, QUEUESUCESS);

            }
            catch (Exception e)
            {
                String message = BagTMLog.LogError(
                    String.Format("BagTM Queue Processing Error with no TTY message", n), this, e);
                if (messages != null && messages[0] != null)
                {
                    ((Message)messages[1]).Label = message.Substring(0, 120);
                    this.SendMessage(messages[1], n, QUEUEERROR);
                } else
                {
                    BagTMLog.LogError(
                       String.Format("BagTM Queue Processing Error with no TTY message", n), this, e);
                }
            }

            BagTMLog.LogDebug(
                    String.Format("BagTM Queue Debug Processing Queue Process End"), this);

        }

        protected object[] ReadQueue(int n)
        {
            object message = null;
            object[] messages = new object[2];

            BagTMLog.LogDebug(
                   String.Format("BagTM Queue Read Task Message Start {0}", n), this);
            Message msg = null;
            try
            {
                if (queueIn.Peek() != null)
                {
                    msg = queueIn.Receive();
                    messages[1] = msg;

                    if (msg != null)
                    {

                        BagTMLog.LogDebug(
                            String.Format("BagTM Queue Read Task {0} for Message Body Type: {1}", n, msg.BodyType), this);

                        message = (object)msg.Body;
                        messages[0] = message;

                        BagTMLog.LogInfo(
                            String.Format("BagTM Queue Message Parsed {0}", message), this);

                        BagTMLog.LogDebug(
                            String.Format("BagTM Queue Read Task {0} with Message {1}", n, msg.Id), this);
                    }
                }
            }
            catch (Exception e)
            {
                String messageError = BagTMLog.LogError(
                        String.Format("BagTM Queue Read Task {0} with Error", n), this, e);
                
                if (msg != null)
                {
                    msg.Label = messageError.Substring(0, 120);
                    this.SendMessage(msg, n, QUEUEERROR);
                }
                throw e;
            }

            BagTMLog.LogDebug(
                   String.Format("BagTM Queue Read Task Message End {0}", n), this);

            
            return messages;
        }

        /// <summary>
        /// Send object to engine message queue
        /// </summary>
        /// <param name="messageObject"></param>
        protected void SendMessage(object messageObject, int n, int queueType)
        {
            BagTMLog.LogDebug(
                   String.Format("BagTM Queue Send Task Message Start {0}", n), this);

            MessageQueue queueUse = this.CreateMessageQueue(queueType);

            // Send a message to the queue.
            if (queueUse.Transactional == true)
            {
                BagTMLog.LogDebug(
                   String.Format("BagTM Queue Create Transaction to Send Task {0}", n), this);

                // Create a transaction.
                MessageQueueTransaction myTransaction = new
                    MessageQueueTransaction();

                BagTMLog.LogDebug(
                   String.Format("BagTM Queue Transaction Created to Send Task {0}", n), this);

                // Begin the transaction.
                myTransaction.Begin();

                BagTMLog.LogDebug(
                   String.Format("BagTM Queue Begin Transaction to Send Task {0}", n), this);

                try
                {
                    // Send the message.
                    queueUse.Send(messageObject, myTransaction);
                }
                catch (Exception)
                {
                    queueUse = this.CreateMessageQueue(queueType);
                    
                    // Send the message.
                    queueUse.Send(messageObject, myTransaction);

                }

                BagTMLog.LogDebug(
                   String.Format("BagTM Queue Message Sent Task {0}", n), this);

                // Commit the transaction.
                myTransaction.Commit();

                BagTMLog.LogInfo(
                        String.Format("BagTM Queue Message Sent {0}", messageObject), this);

                BagTMLog.LogDebug(
                   String.Format("BagTM Queue Transaction Commited Task {0}", n), this);
            }
            else
            {
                BagTMLog.LogDebug(
                   String.Format("BagTM Queue No Transaction to Send Task {0}", n), this);

                queueUse = this.CreateMessageQueue(queueType);

                queueUse.Send(messageObject);

                BagTMLog.LogInfo(
                        String.Format("BagTM Queue Message Sent {0}", messageObject), this);

                BagTMLog.LogDebug(
                   String.Format("BagTM Queue Message Sent No Transaction Task {0}", n), this);
            }

            return;
        }

        /// <summary>
        /// Create all message queue for processes
        /// </summary>
        /// <param name="queueType"></param>
        /// <returns></returns>
        private MessageQueue CreateMessageQueue(int queueType)
        {
            switch (queueType)
            {
                case QUEUESUCESS: //Sucess
                    queueInSucess = new MessageQueue(this.GetAppSetting(config, "queueInSucessName") + isDebug);
                    BagTMLog.LogDebug("BagTM Queue Processing QueueInSucess", queueInSucess);
                    return queueInSucess;

                case QUEUEERROR: //Error
                    queueInError = new MessageQueue(this.GetAppSetting(config, "queueInErrorName") + isDebug);
                    BagTMLog.LogDebug("BagTM Queue Processing QueueInError", queueInError);
                    return queueInError;

                case QUEUEENGINE: //Engine
                    queueEngine = new MessageQueue(this.GetAppSetting(config, "queueEngineName") + isDebug);
                    ((XmlMessageFormatter)queueEngine.Formatter).TargetTypes = new Type[2] {
                    typeof(BagTMDBLibrary.OSUSR_UUK_BAGMSGS), typeof(BagTMDBLibrary.OSUSR_UUK_PAXMSGS) };
                    BagTMLog.LogDebug("BagTM Queue Processing QueueEngine", queueEngine);
                    return queueEngine;

                case QUEUEIN: //In
                    queueIn = new MessageQueue(this.GetAppSetting(config, "queueInName") + isDebug);
                    queueIn.Formatter = messageFormater;
                    BagTMLog.LogDebug("BagTM Queue Processing QueueIn", queueIn);
                    return queueIn;

                default:
                    throw new QueueProcessingException("Send Message to unkown queue");
            }
        }

        private String GetAppSetting(Configuration config, string key)
        {
            KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            if (element != null)
            {
                string value = element.Value;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }

    }
}
