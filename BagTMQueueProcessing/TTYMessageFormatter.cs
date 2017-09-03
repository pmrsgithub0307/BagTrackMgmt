using BagTMDBLibrary;
using System;
using System.IO;
using System.Messaging;

namespace BagTMQueueProcessing
{
    class TTYMessageFormatter : IMessageFormatter, ICloneable 
    {
        /// <summary>
        /// Message parser logic
        /// </summary>
        private IBaggageMessageParsing parserBag;
        private PTMMessageParsing parserPTM;

        /// <summary>
        /// Entity framework database baggage context
        /// </summary>
        private BaggageEntities db;

        /// <summary>
        /// TTY Parsing logic
        /// </summary>
        private TTYCollection ttyCollection;

        private static String BagParser = "BAG";
        private static String PTMParser = "PTM";

        /// <summary>
        /// Constructor for the Baggage TTY parser
        /// </summary>
        public TTYMessageFormatter ()
        {
            
        }

        /// <summary>
        /// Constructor for the Baggage TTY parser with TTY parsing logic
        /// </summary>
        public TTYMessageFormatter(TTYCollection ttyCollection, BaggageEntities db, String hub)
        {
            this.ttyCollection = ttyCollection;
            this.parserBag = new BaggageMessageParsing(ttyCollection);
            this.parserPTM = new PTMMessageParsing(ttyCollection, hub);
            this.db = db;
        }

        /// <summary>
        /// Implementation of CanRead IMessagerFormatter
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool CanRead(Message message)
        {
            return true;
        }

        /// <summary>
        /// Implementation of the Read IMessagerFormatter. This reads Baggage TTY messages for the Bag Track Management windows service.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public object Read(Message message)
        {
            StreamReader sr = new StreamReader(message.BodyStream);
            string messageBody = "";
            while (sr.Peek() >= 0)
            {
                messageBody += sr.ReadToEnd();
            }

            messageBody = this.RemoveHeaders(messageBody);

            if (messageBody != null && messageBody.Length > 2 && ttyCollection.HasTTY(messageBody.Substring(0, 3)))
            {
                String keyTTY = messageBody.Substring(0, 3);

                if (BagParser.Equals(ttyCollection.TTYType(keyTTY).ToUpper()))
                {
                    return parserBag.parse(messageBody, db);
                } 
                else if (PTMParser.Equals(ttyCollection.TTYType(keyTTY).ToUpper()))
                {
                    return parserPTM.parse(messageBody, db);
                }
                else
                {
                    throw new QueueProcessingException("Unknow message type");
                }
            }
            else
            {
                throw new QueueProcessingException("Unknow message type");
            }
        }

        /// <summary>
        /// Implementation of the Write IMessagerFormatter. Not used for Bag Track Management Service.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="obj"></param>
        public void Write(Message message, object obj)
        {
            
        }

        /// <summary>
        /// Implementation of the Clone IClone.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return this;
        }

        private String RemoveHeaders(String message)
        {
            int indexOfSMI = 99999;

            foreach (TTYConfigElement config in ttyCollection)
            {
                if (message.IndexOf(config.key) > -1)
                    indexOfSMI = Math.Min(indexOfSMI, message.IndexOf(config.key));
            }

            return (indexOfSMI > 1) ? message.Remove(0, indexOfSMI) : message;

        }
    }
}
