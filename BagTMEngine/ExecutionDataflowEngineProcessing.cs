using System;
using System.Threading.Tasks.Dataflow;
using System.Messaging;
using System.Configuration;
using BagTMDBLibrary;
using System.Linq;
using System.Collections.Generic;
using NRules;
using NRules.Fluent;
using System.Reflection;
using BagTMEngineProcessing.Rules;
using System.Data.Entity;
using BagTMCommon;
using System.Data.Entity.Validation;

namespace BagTMEngineProcessing
{
    public class ExecutionDataflowEngineProcessing : IExecutionEngineProcessing
    {
        /// <summary>
        /// Number of parallel task for queue processing
        /// </summary>
        private int parallelismCounter;

        /// <summary>
        /// TTY baggage tty queue replica of Amadeus queue
        /// </summary>
        private MessageQueue queueEngine;
        private MessageQueue queueEngineError;
        private MessageQueue queueEngineSucess;

        /// <summary>
        /// NRules session factory
        /// </summary>
        ISessionFactory factory;

        private const int QUEUESUCESS = 1;
        private const int QUEUEERROR = 2;
        private const int QUEUEENGINE = 3;

        /// <summary>
        /// Timelimit to wait for new messages in the queue. Assumption that timer on service will pick after
        /// </summary>
        private TimeSpan timeSpan;

        /// <summary>
        /// Baggage Terminal Code
        /// </summary>
        private String baggageTerminalCode;

        /// <summary>
        /// Sorter Timer in minutes
        /// </summary>
        private Int32 sorterTime;

        /// <summary>
        /// Estimated Time to Close Gate
        /// </summary>
        private Int32 etcg;

        /// <summary>
        /// Default Equipment Type for Reference Tables
        /// </summary>
        private String defaultEquipment;

        /// <summary>
        /// Default From for Reference Tables
        /// </summary>
        private String defaultStandFrom;

        /// <summary>
        /// Default To for Reference Tables
        /// </summary>
        private String defaultGateTo;

        /// <summary>
        /// Default Max Passengers Turnaround
        /// </summary>
        private Int32 maxPaxTurnaround;

        /// <summary>
        /// Default Max Baggage Turnaround
        /// </summary>
        private Int32 maxBaggageTurnaround;

        /// <summary>
        /// Sorter Max ThroughPut Baggages / Minute
        /// </summary>
        private Int32 maxSorterThroughPut;

        /// <summary>
        /// Min Load / Unload Time
        /// </summary>
        private Int32 minLoadUnloadTime;

        /// <summary>
        /// Max Load / Unload Time
        /// </summary>
        private Int32 maxLoadUnloadTime;

        /// <summary>
        /// Hub code for Baggage Integrity
        /// </summary>
        private String hub;

        /// <summary>
        /// Get the application configuration file.
        /// </summary>
        Configuration config;

        /// <summary>
        /// Airline code for Baggage Integrity
        /// </summary>
        private String airline;

        /// <summary>
        /// Map to store sorter working volumes
        /// </summary>
        private SorterProcessingVolumeMap sorterVolumeMap;

        /// <summary>
        /// Reference tables
        /// </summary>
        List<OSUSR_UUK_BAGTIMESREFERENCE> refList;
        List<OSUSR_UUK_EQUIPTYPE> equipementTypeList;
        List<OSUSR_UUK_TAXITIMES> taxiTimesList;
        List<OSUSR_UUK_REGISTRATIONS> registrationList;

        /// <summary>
        /// Controles action block paralellism
        /// </summary>
        private bool isProcessing = false;

        /// <summary>
        /// Engines processing
        /// </summary>
        private H2HProcessing h2hProcessing = null;
        private PTMH2HProcessing ptmH2HProcessing = null;
        private BagIntegrityProcessing bagIntergityProcessing = null;
        private FLTINFOProcessing fltInfoProcessing = null;

        /// <summary>
        /// Control debug execution
        /// </summary>
        private String isDebug = "";

        /// <summary>
        /// Constructor to create logger and tasks and queue parametrization
        /// </summary>
        public ExecutionDataflowEngineProcessing()
        {
            BagTMLog.LogDebug("BagTM Engine Processing Constructor", this);
            
            config = ConfigurationManager.OpenExeConfiguration(
                        ConfigurationUserLevel.None) as Configuration;

            hub = this.GetAppSetting(config, "hub");
            if (hub == null) throw new EngineProcessingException("No hub defined to process TTY messages");

            airline = this.GetAppSetting(config, "airline");
            if (airline == null) throw new EngineProcessingException("No airline defined to process TTY messages");

            BagTMLog.LogDebug("BagTM Engine Processing hub ", hub);

            String timeSpanParameter = this.GetAppSetting(config, "timeSpan");
            int timeSpanNumber = 0;
            if (!int.TryParse(timeSpanParameter, out timeSpanNumber))
                throw new EngineProcessingException("Parameter TimeSpan incorrectly configured");

            this.timeSpan = new TimeSpan((Int64)(timeSpanNumber * TimeSpan.TicksPerMillisecond));

            BagTMLog.LogDebug("BagTM Engine Processing Constructor timeSpanNumber", this.timeSpan);

            String parallelismCounterParameter = this.GetAppSetting(config, "parallelismCounter");
            if (!int.TryParse(parallelismCounterParameter, out this.parallelismCounter))
                throw new EngineProcessingException("Parameter ParallelismCounter incorrectly configured");

            BagTMLog.LogDebug("BagTM Engine Processing Constructor parallelismCounterParameter", parallelismCounterParameter);

            queueEngine = this.CreateMessageQueue(QUEUEENGINE);
            queueEngineSucess = this.CreateMessageQueue(QUEUESUCESS);
            queueEngineError = this.CreateMessageQueue(QUEUEERROR);

            BagTMLog.LogDebug("BagTM Engine Processing Constructor queueEngine", queueEngine);
            BagTMLog.LogDebug("BagTM Engine Processing Constructor queueEngineSucess", queueEngineSucess);
            BagTMLog.LogDebug("BagTM Engine Processing Constructor queueEngineError", queueEngineError);

            baggageTerminalCode = this.GetAppSetting(config, "baggageTerminal");
            if (baggageTerminalCode == null) throw new EngineProcessingException("No baggage terminal configured");

            String sorterTimeParameter = this.GetAppSetting(config, "sorterTime");
            if (!int.TryParse(sorterTimeParameter, out sorterTime))
                throw new EngineProcessingException("Parameter sorter time incorrectly configured");

            String etcgParameter = this.GetAppSetting(config, "etcg");
            if (!int.TryParse(sorterTimeParameter, out etcg))
                throw new EngineProcessingException("Parameter estimated time to close gate incorrectly configured");

            defaultEquipment = this.GetAppSetting(config, "defaultEquipment");
            if (!int.TryParse(defaultEquipment, out int aux))
                throw new EngineProcessingException("Parameter default equipment incorrectly configured");

            defaultStandFrom = this.GetAppSetting(config, "defaultStandFrom");
            if (defaultStandFrom == null) throw new EngineProcessingException("No default stand from configured");

            defaultGateTo = this.GetAppSetting(config, "defaultGateTo");
            if (defaultGateTo == null) throw new EngineProcessingException("No default stand to configured");

            String maxPaxTurnaroundParameter = this.GetAppSetting(config, "maxPaxTurnaround");
            if (!int.TryParse(maxPaxTurnaroundParameter, out this.maxPaxTurnaround))
                throw new EngineProcessingException("No max pax turnaround configured");

            String maxBaggageTurnaroundParameter = this.GetAppSetting(config, "maxBaggageTurnaround");
            if (!int.TryParse(maxBaggageTurnaroundParameter, out this.maxBaggageTurnaround))
                throw new EngineProcessingException("No max baggage turnaround configured");

            String maxSorterThroughPutParameter = this.GetAppSetting(config, "maxSorterThroughPut");
            if (!int.TryParse(maxSorterThroughPutParameter, out this.maxSorterThroughPut))
                throw new EngineProcessingException("No max sorter through put configured");

            String minLoadUnloadTimeParameter = this.GetAppSetting(config, "minLoadUnloadTime");
            if (!int.TryParse(minLoadUnloadTimeParameter, out this.minLoadUnloadTime))
                throw new EngineProcessingException("No min load / unload configured");

            String maxLoadUnloadTimeParameter = this.GetAppSetting(config, "maxLoadUnloadTime");
            if (!int.TryParse(maxLoadUnloadTimeParameter, out this.maxLoadUnloadTime))
                throw new EngineProcessingException("No max load / unload configured");
            
            //Load rules
            var repository = new RuleRepository();
            Type[] ruleTypes = new Type[13];
            repository.Load(x => x
                .From(Assembly.GetExecutingAssembly()).To("BagTMRuleSet")
                .Where(r => r.IsTagged("BagIntegrity")));

            BagTMLog.LogDebug("BagTM Engine Processing Constructor repository", repository);

            //Compile rules
            var ruleSets = repository.GetRuleSets();
            var compiler = new RuleCompiler();
            factory = compiler.Compile(ruleSets);

            //Create a working session
            factory.Events.FactInsertedEvent += BagRulesEventMonitorization.OnFactInsertedEvent;
            factory.Events.FactUpdatedEvent += BagRulesEventMonitorization.OnFactUpdatedEvent;
            factory.Events.FactRetractedEvent += BagRulesEventMonitorization.OnFactRetractedEvent;
            factory.Events.ActivationCreatedEvent += BagRulesEventMonitorization.OnActivationCreatedEvent;
            factory.Events.ActivationUpdatedEvent += BagRulesEventMonitorization.OnActivationUpdatedEvent;
            factory.Events.ActivationDeletedEvent += BagRulesEventMonitorization.OnActivationDeletedEvent;
            factory.Events.RuleFiringEvent += BagRulesEventMonitorization.OnRuleFiringEvent;
            factory.Events.RuleFiredEvent += BagRulesEventMonitorization.OnRuleFireEvent;
            factory.Events.ActionFailedEvent += BagRulesEventMonitorization.OnActionFailedEvent;
            factory.Events.ConditionFailedEvent += BagRulesEventMonitorization.OnConditionFailedEvent;

            this.sorterVolumeMap = new SorterProcessingVolumeMap();

            BagTMLog.LogDebug("BagTM Engine Processing Constructor factory", factory);

            this.RefreshRefLists();

            BagTMLog.LogDebug(
                    String.Format("BagTM Engine Processing Task Process Message Define Dataflow Block Parallelism Counter {0}", parallelismCounter)
                    , this);

            BagTMLog.LogDebug("BagTM Engine Processing Constructor Ending", this);
        }

        /// <summary>
        /// Task creation for engine processing events
        /// </summary>
        public void EngineProcessing()
        {
            BagTMLog.LogDebug(
                    String.Format("BagTM Engine Processing Queue Process Start "), this);

            if (this.isProcessing)
            {
                BagTMLog.LogDebug(
                    String.Format("BagTM Engine Processing Queue Process Is Processing"), this);
                return;
            }

            BagTMLog.LogDebug(
                    String.Format("BagTM Engine Processing Task Process Message Define Dataflow Block Number of Messages {0}", parallelismCounter)
                    , this);

            Action<int> engineProcess = n =>
            {
                BagTMLog.LogDebug(
                    String.Format("BagTM Engine Processing Task Process Message {0}", n), this);

                EngineProcess(n);
                EngineErrorReprocess(n);

            };

            var opts = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = parallelismCounter
            };

            var actionBlock = new ActionBlock<int>(engineProcess, opts);

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
                    String.Format("BagTM Engine Processing Queue Process End "), this);
        }

        /// <summary>
        /// Engine processing for debug
        /// </summary>
        public void EngineProcessingDebug()
        {
            isDebug = "_debug";

            queueEngine = this.CreateMessageQueue(QUEUEENGINE);
            queueEngineSucess = this.CreateMessageQueue(QUEUESUCESS);
            queueEngineError = this.CreateMessageQueue(QUEUEERROR);

            EngineProcess(0);
            //EngineErrorReprocessAll(0);
        }

        /// <summary>
        /// Engine processing for debug
        /// </summary>
        public void EngineProcess(int n)
        {
            BagTMLog.LogDebug(
                    String.Format("BagTM Engine Debug Processing Queue Process Start"), this);

            object[] messages = null;
            try
            {
                this.RefreshRefLists();

                messages = this.ReadQueue(n, QUEUEENGINE);

                if (messages == null) return;

                if (messages[0].GetType().Equals(typeof(OSUSR_UUK_BAGMSGS)))
                {
                    this.bagIntergityProcessing.ProcessBaggageTTYEngine(messages[0], n);
                    Message messageSucess = new Message(messages[0]);
                    messageSucess.Label = "Bag Msgs: " + ((OSUSR_UUK_BAGMSGS)messages[0]).ID;
                    this.SendMessage(messageSucess, n, QUEUESUCESS);
                }
                else if (messages[0].GetType().Equals(typeof(OSUSR_UUK_PAXMSGS)))
                {
                    this.ptmH2HProcessing.ProcessPTMTTYEngine(messages[0], n);
                    Message messageSucess = new Message(messages[0]);
                    messageSucess.Label = "Pax Msgs: " + ((OSUSR_UUK_PAXMSGS)messages[0]).ID;
                    this.SendMessage(messageSucess, n, QUEUESUCESS);
                }
                else if (messages[0].GetType().Equals(typeof(OSUSR_UUK_FLT_INFO)))
                {
                    this.fltInfoProcessing.ProcessFLTINFOEngine(messages[0], n);
                    Message messageSucess = new Message(messages[0]);
                    messageSucess.Label = "Flt Msgs: " + ((OSUSR_UUK_FLT_INFO)messages[0]).ID;
                    this.SendMessage(messageSucess, n, QUEUESUCESS);
                }
                else
                {
                    BagTMLog.LogDebug(
                        String.Format("BagTM Engine Processing Task Process Message read task {0} with message {1} no engine to process message", n, messages[0].GetType()), this);
                    ((Message)messages[1]).Label = String.Format("BagTM Engine Processing Task Process Message read task {0} with message {1} no engine to process message", n, messages[0].GetType()).Substring(0, 120);
                    this.SendMessage(messages[1], n, QUEUEERROR);
                }

            }
            catch (Exception e)
            {
                String message = BagTMLog.LogError(
                    String.Format("BagTM Engine Processing Task {0} Message", n), this, e);
                if (messages != null && messages[0] != null)
                {
                    ((Message)messages[1]).Label = message.Substring(0, 120);
                    this.SendMessage(messages[1], n, QUEUEERROR);
                }
            }
        }

        /// <summary>
        /// Read queue
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        protected object[] ReadQueue(int n, int queueType)
        {
            object message = null;
            object[] messages = new object[2];

            BagTMLog.LogDebug(
                   String.Format("BagTM Engine Read Task Message Start {0}", n), this);

            Message msg = null;

            try
            {
                MessageQueue queueUse = this.CreateMessageQueue(queueType);

                try
                {
                    msg = queueUse.Receive(this.timeSpan);
                    messages[1] = msg;
                }
                catch(Exception e)
                {
                    if (e.Message.Contains("Timeout")) return null;
                    queueUse = this.CreateMessageQueue(queueType);

                    msg = queueUse.Receive(this.timeSpan);
                    messages[1] = msg;
                }

                if (msg != null)
                {
                    BagTMLog.LogDebug(
                        String.Format("BagTM Engine Read Task {0} for Message Body Type: {1}", n, msg.BodyType), this);

                    message = (Object)msg.Body;
                    messages[0] = message;

                    BagTMLog.LogInfo("BagTM Engine TTY Message: " + message, this);

                    BagTMLog.LogDebug(
                        String.Format("BagTM Engine Read Task {0} with Message {1}", n, msg.Id), this);
                }
            }
            catch (Exception e)
            {
                BagTMLog.LogError(
                        String.Format("BagTM Engine Read Task {0} with Error", n), this, e);

                if (msg != null) this.SendMessage(msg, n, QUEUEERROR);

                throw e;
            }

            BagTMLog.LogDebug(
                   String.Format("BagTM Engine Read Task Message End {0}", n), this);

            return messages;
        }

        /// <summary>
        /// Engine processing for debug
        /// </summary>
        public void EngineErrorReprocessAll(int n)
        {
            BagTMLog.LogDebug(
                    String.Format("BagTM Engine Debug Reprocessing All Queue Process Start"), this);

            EngineErrorReprocess(n);

            BagTMLog.LogDebug(
                    String.Format("BagTM Engine Debug Reprocessing All Queue Process End"), this);
        }
        /// <summary>
        /// Engine processing for debug
        /// </summary>
        public void EngineErrorReprocess(int n)
        {
            BagTMLog.LogDebug(
                    String.Format("BagTM Engine Debug Reprocessing Queue Process Start"), this);

            object[] messages = null;
            try
            {
                messages = this.ReadQueue(n, QUEUEERROR);

                if (messages == null) return;

                if (messages[0].GetType().Equals(typeof(OSUSR_UUK_BAGMSGS)))
                {
                    this.bagIntergityProcessing.CalculateErrorReprocessing((OSUSR_UUK_BAGMSGS)messages[0], n);
                    Message messageSucess = new Message(messages[0]);
                    messageSucess.Label = "Bag Msgs: " + ((OSUSR_UUK_BAGMSGS)messages[0]).ID;
                    this.SendMessage(messageSucess, n, QUEUESUCESS);
                }
                else if (messages[0].GetType().Equals(typeof(OSUSR_UUK_PAXMSGS)))
                {
                    this.ptmH2HProcessing.CalculateErrorReprocessing((OSUSR_UUK_PAXMSGS)messages[0], n);
                    Message messageSucess = new Message(messages[0]);
                    messageSucess.Label = "Pax Msgs: " + ((OSUSR_UUK_PAXMSGS)messages[0]).ID;
                    this.SendMessage(messageSucess, n, QUEUESUCESS);
                }
                else if (messages[0].GetType().Equals(typeof(OSUSR_UUK_FLT_INFO)))
                {
                    this.fltInfoProcessing.ProcessFLTINFOEngine(messages[0], n);
                    Message messageSucess = new Message(messages[0]);
                    messageSucess.Label = "Flt Msgs: " + ((OSUSR_UUK_FLT_INFO)messages[0]).ID;
                    this.SendMessage(messageSucess, n, QUEUESUCESS);
                }
                else
                {
                    BagTMLog.LogDebug(
                        String.Format("BagTM Engine Reprocessing Task Process Message read task {0} with message {1} no engine to process message", n, messages[0].GetType()), this);
                    ((Message)messages[1]).Label = String.Format("BagTM Engine Reprocessing Task Process Message read task {0} with message {1} no engine to process message", n, messages[0].GetType()).Substring(0, 120);
                    this.SendMessage(messages[1], n, QUEUEERROR);
                }

            }
            catch (Exception e)
            {
                String message = BagTMLog.LogError(
                    String.Format("BagTM Engine Reprocessing Task {0} Message", n), this, e);
                if (messages != null && messages[0] != null)
                {
                    ((Message)messages[1]).Label = message.Substring(0, 120);
                    this.SendMessage(messages[1], n, QUEUEERROR);
                }
            }
        }
        
        /// <summary>
        /// Obtain flight baggage times 
        /// </summary>
        /// <returns></returns>
        private List<OSUSR_UUK_BAGTIMESREFERENCE> SearchBagTimesReference()
        {
            BaggageEntities db = new BaggageEntities();

            var bagsTimesReference = from s in db.OSUSR_UUK_BAGTIMESREFERENCE
                                     select s;

            var results = bagsTimesReference.ToList<OSUSR_UUK_BAGTIMESREFERENCE>();

            return results;
        }

        /// <summary>
        /// Obtain equipement type
        /// </summary>
        /// <returns></returns>
        private List<OSUSR_UUK_EQUIPTYPE> SearchEquipementsTypes()
        {
            BaggageEntities db = new BaggageEntities();

            var equipTypes = from s in db.OSUSR_UUK_EQUIPTYPE
                             select s;

            var results = equipTypes.ToList<OSUSR_UUK_EQUIPTYPE>();

            return results;
        }

        /// <summary>
        /// Obtain aircraft registration
        /// </summary>
        /// <returns></returns>
        private List<OSUSR_UUK_REGISTRATIONS> SearchRegistration()
        {
            BaggageEntities db = new BaggageEntities();

            var registration = from s in db.OSUSR_UUK_REGISTRATIONS
                               select s;

            var results = registration.ToList<OSUSR_UUK_REGISTRATIONS>();

            return results;
        }
        
        /// <summary>
        /// Send object to engine message queue
        /// </summary>
        /// <param name="messageObject"></param>
        protected void SendMessage(object messageObject, int n, int queueType)
        {
            BagTMLog.LogDebug(
                   String.Format("BagTM Engine Send Task Message Start {0}", n), this);

            MessageQueue queueUse = this.CreateMessageQueue(queueType);

            // Send a message to the queue.
            if (queueUse.Transactional == true)
            {
                BagTMLog.LogDebug(
                   String.Format("BagTM Engine Create Transaction to Send Task {0}", n), this);

                // Create a transaction.
                MessageQueueTransaction myTransaction = new
                    MessageQueueTransaction();

                BagTMLog.LogDebug(
                   String.Format("BagTM Engine Transaction Created to Send Task {0}", n), this);

                // Begin the transaction.
                myTransaction.Begin();

                BagTMLog.LogDebug(
                   String.Format("BagTM Engine Begin Transaction to Send Task {0}", n), this);

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
                   String.Format("BagTM Engine Message Sent Task {0}", n), this);

                // Commit the transaction.
                myTransaction.Commit();

                BagTMLog.LogInfo(
                        String.Format("BagTM Engine Message Sent {0}", messageObject), this);

                BagTMLog.LogDebug(
                   String.Format("BagTM Engine Transaction Commited Task {0}", n), this);
            }
            else
            {
                BagTMLog.LogDebug(
                   String.Format("BagTM Engine No Transaction to Send Task {0}", n), this);

                queueUse.Send(messageObject);

                BagTMLog.LogInfo(
                        String.Format("Engine Queue Message Sent {0}", messageObject), this);

                BagTMLog.LogDebug(
                   String.Format("BagTM Engine Message Sent No Transaction Task {0}", n), this);
            }

            return;
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


        /// <summary>
        /// Search taxi times for flight
        /// </summary>
        /// <returns></returns>
        private List<OSUSR_UUK_TAXITIMES> SearchTaxiTimes()
        {
            BaggageEntities db = new BaggageEntities();

            var taxiTimes = from s in db.OSUSR_UUK_TAXITIMES
                            select s;

            var results = taxiTimes.ToList<OSUSR_UUK_TAXITIMES>();

            return results;
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
                    queueEngineSucess = new MessageQueue(this.GetAppSetting(config, "queueEngineSucessName") + isDebug);
                    ((XmlMessageFormatter)queueEngineSucess.Formatter).TargetTypes = new Type[3] {
                    typeof(BagTMDBLibrary.OSUSR_UUK_BAGMSGS), typeof(BagTMDBLibrary.OSUSR_UUK_PAXMSGS), typeof(BagTMDBLibrary.OSUSR_UUK_FLT_INFO) };
                    return queueEngineSucess;

                case QUEUEERROR: //Error
                    queueEngineError = new MessageQueue(this.GetAppSetting(config, "queueEngineErrorName") + isDebug);
                    ((XmlMessageFormatter)queueEngineError.Formatter).TargetTypes = new Type[3] {
                    typeof(BagTMDBLibrary.OSUSR_UUK_BAGMSGS), typeof(BagTMDBLibrary.OSUSR_UUK_PAXMSGS), typeof(BagTMDBLibrary.OSUSR_UUK_FLT_INFO) };
                    return queueEngineError;

                case QUEUEENGINE: //Engine
                    queueEngine = new MessageQueue(this.GetAppSetting(config, "queueEngineName") + isDebug);
                    ((XmlMessageFormatter)queueEngine.Formatter).TargetTypes = new Type[3] {
                    typeof(BagTMDBLibrary.OSUSR_UUK_BAGMSGS), typeof(BagTMDBLibrary.OSUSR_UUK_PAXMSGS), typeof(BagTMDBLibrary.OSUSR_UUK_FLT_INFO) };
                    return queueEngine;

                default:
                    throw new EngineProcessingException("Send Message to unkown queue");
            }
        }

        /// <summary>
        /// Refresh reference list to ensure 
        /// </summary>
        private void RefreshRefLists ()
        {
            try
            {
                // BagTimes Reference
                if (this.refList == null) this.refList = this.SearchBagTimesReference();

                BagTMLog.LogDebug("BagTM Engine Processing Constructor refList", refList);

                // Equipements Types
                if (this.equipementTypeList == null) this.equipementTypeList = this.SearchEquipementsTypes();

                BagTMLog.LogDebug("BagTM Engine Processing Constructor equipementTypeList", equipementTypeList);

                // Taxi Times 
                if (this.taxiTimesList == null) this.taxiTimesList = this.SearchTaxiTimes();

                BagTMLog.LogDebug("BagTM Engine Processing Constructor taxiTimesList", taxiTimesList);

                // Registrations
                if (this.registrationList == null) this.registrationList = this.SearchRegistration();

                BagTMLog.LogDebug("BagTM Engine Processing Constructor registrationList", registrationList);

            }
            catch (DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The query errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new EngineProcessingException(exceptionMessage);
            }

            if(this.h2hProcessing == null)
                this.h2hProcessing = new H2HProcessing(this.hub, this.defaultEquipment, this.defaultStandFrom,
                                    this.defaultGateTo, this.baggageTerminalCode, this.minLoadUnloadTime, this.maxLoadUnloadTime,
                                    this.maxBaggageTurnaround, this.sorterVolumeMap, this.maxSorterThroughPut, this.sorterTime,
                                    this.refList, this.equipementTypeList, this.taxiTimesList, this.registrationList);

            if (this.ptmH2HProcessing == null)
                this.ptmH2HProcessing = new PTMH2HProcessing(this.hub, this.etcg, this.maxPaxTurnaround, this.airline);

            if (this.bagIntergityProcessing == null)
                this.bagIntergityProcessing = new BagIntegrityProcessing(this.hub, this.airline, this.factory, this.h2hProcessing);

            if (this.fltInfoProcessing == null)
                this.fltInfoProcessing = new FLTINFOProcessing(this.h2hProcessing, this.ptmH2HProcessing);
        }

        private List<T> CreateEmptyGenericList<T>(T example)
        {
            return new List<T>();
        }
    }
}
