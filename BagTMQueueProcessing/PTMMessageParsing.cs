using System;
using System.Messaging;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using BagTMDBLibrary;
using BagTMCommon;

namespace BagTMQueueProcessing
{
    
    class PTMMessageParsing : IBaggageMessageParsing
    {
        /// <summary>
        /// TTY Parsing logic
        /// </summary>
        private TTYCollection ttyCollection;

        private String hub;

        /// <summary>
        /// TTY message element divider
        /// </summary>
        private String matchFlights = @"PTM\r\n=TEXT\r\n[A-Z]{2}[0-9]{3,4}/[0-9]{1,2}[A-Z]{3}\s[A-Z]{3}[A-Z]{3}\sPART1\r\n";

        /// <summary>
        /// TTY message components divider
        /// </summary>
        private String matchOutbounds = @"[A-Z]{2}[0-9]{3,4}(/[0-9]{2})?(/S)?(/N)?\s[A-Z]{3}\s[0-9]{1,3}[A-Z]\s[0-9]{1,3}B[0-9]{1,3}K(\s[A-Z/ ]*)?(\.CHD[0-9]{1,2})?(\.INF[0-9]{1,2})?(\.RQ)?(\.SA)?\r\n";


        public PTMMessageParsing(TTYCollection ttyCollection, String hub)
        {
            BagTMLog.LogDebug("BagTM Queue Message Parsing Constructor", this);

            this.ttyCollection = ttyCollection;
            this.hub = hub;
            BagTMLog.LogDebug("BagTM Queue Message Parsing Constructor Ending", this);

        }
        

        public Object parse(String messageString, BaggageEntities db)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Queue Message Parsing PTM TTY {0}", messageString), this);

            Match flight;
            MatchCollection outbounds;
            IPTMTTYTable messageObject = null;
            String methodName;
            String parameter = null;
            String messageStripped;

            // At this stage formatter alredy removed headers
            messageStripped = this.RemoveHeaders(messageString);
            flight = Regex.Match(messageString, matchFlights);
            outbounds = Regex.Matches(messageString.Remove(0, flight.Length), matchOutbounds);

            try
            {
                BagTMLog.LogDebug(
                    String.Format("BagTM Queue Message Parsing PTM TTY Message flight {0}", flight.Value), this);

                PTMTTYTable paxmsgs = new PTMTTYTable();

                TTYComponentConfigElement configFlight = ttyCollection.GetTTYElement("PTM").ttyElements.GetTTYElementElement("default").ttycomponents.GetTTYElementElement("Flight");
                
                if (!flight.Success)
                    throw new PTMParsingException("No flight information impossible to process PTM message.");

                DateTime messageTimestamp = DateTime.Now;

                if (outbounds.Count > 0)
                {
                    foreach (Match outbound in outbounds)
                    {

                        messageObject = (IPTMTTYTable)Activator.CreateInstance(Type.GetType(ttyCollection.GetTTYElement("PTM").entityName));

                        BagTMLog.LogDebug(
                                String.Format("BagTM Queue Message Parsing PTM TTY Message flight {0}", flight.Value), this);

                        parameter = flight.Value.Substring(4, flight.Length - 10);
                        methodName = configFlight.methodName;
                        // Not necessary if parameter is null since no change to object
                        if (parameter != null) messageObject.SetValue(methodName, parameter);

                        TTYComponentConfigElement configOutbound = ttyCollection.GetTTYElement("PTM").ttyElements.GetTTYElementElement("default").ttycomponents.GetTTYElementElement("Outbounds");

                        BagTMLog.LogDebug(
                            String.Format("BagTM Queue Message Parsing PTM TTY Message flight {0} and outbound {1}", flight.Value, outbound.Value), this);
                        parameter = outbound.Value;
                        methodName = configOutbound.methodName;
                        // Not necessary if parameter is null since no change to object
                        if (parameter != null) messageObject.SetValue(methodName, parameter);

                        // Create timestamp message identifier
                        messageObject.SetValue("Stamp", messageTimestamp.ToString());

                        // Se o objeto estiver instanciado então foi preenchido e deve ser guardado em base de dados
                        if (messageObject != null)
                        {
                            BagTMLog.LogInfo(
                                    String.Format("BagTM Queue Message PTM Message Outbound Parsed {0}", messageObject), this);
                            messageObject.Save(db, hub);
                        }
                    }

                }
                else
                {
                    messageObject = (IPTMTTYTable)Activator.CreateInstance(Type.GetType(ttyCollection.GetTTYElement("PTM").entityName));

                    BagTMLog.LogDebug(
                            String.Format("BagTM Queue Message Parsing PTM TTY Message flight {0}", flight.Value), this);

                    parameter = flight.Value.Substring(4, flight.Length - 10);
                    methodName = configFlight.methodName;
                    // Not necessary if parameter is null since no change to object
                    if (parameter != null) messageObject.SetValue(methodName, parameter);

                    // Se o objeto estiver instanciado então foi preenchido e deve ser guardado em base de dados
                    if (messageObject != null)
                    {
                        BagTMLog.LogInfo(
                                String.Format("BagTM Queue Message PTM Message Parsed {0}", messageObject), this);
                        messageObject.Save(db, hub);
                    }
                }

                return ((PTMTTYTable)messageObject).getOSUSR_UUK_PAXMSGS();

            } catch (Exception e)
            {
                BagTMLog.LogError("BagTM Queue Message Parsing PTM TTY Parser Error", this, e);
                
                throw e;
            }
            
        }

        private String RemoveHeaders (String message)
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
