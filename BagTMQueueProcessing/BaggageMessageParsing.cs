using System;
using System.Messaging;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using BagTMDBLibrary;
using BagTMCommon;

namespace BagTMQueueProcessing
{
    
    class BaggageMessageParsing : IBaggageMessageParsing
    {
        /// <summary>
        /// TTY Parsing logic
        /// </summary>
        private TTYCollection ttyCollection;

        /// <summary>
        /// TTY message element divider
        /// </summary>
        private String matchElements = @"\.";

        /// <summary>
        /// TTY message components divider
        /// </summary>
        private String matchComponents = @"/";


        public BaggageMessageParsing(TTYCollection ttyCollection)
        {
            BagTMLog.LogDebug("BagTM Queue Message Parsing Constructor", this);

            this.ttyCollection = ttyCollection;

            BagTMLog.LogDebug("BagTM Queue Message Parsing Constructor Ending", this);

        }
        

        public Object parse(String messageString, BaggageEntities db)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Queue Message Parsing TTY {0}", messageString), this);

            String[] elements;
            String[] components;
            String keyTTY;
            String keyElement;
            IBaggageTTYTable messageObject = null;
            String methodName;
            String parameter = null;
            String messageStripped;

            // At this stage formatter alredy removed headers
            messageStripped = this.RemoveHeaders(messageString);
            elements = Regex.Split(messageStripped, matchElements);

            try
            {
                BagTMLog.LogDebug(
                    String.Format("BagTM Queue Message Parsing TTY Message element[0] {0} element[0].lenght {1}",
                            elements[0], elements[0].Length), this);
                
                if (elements[0] != null && elements[0].Length > 2 && ttyCollection.HasTTY(elements[0].Substring(0, 3)))
                {
                    keyTTY = elements[0].Substring(0,3);

                    BagTMLog.LogDebug(
                        String.Format("BagTM Queue Message Parsing TTY Message with Key {0} Processing by EntityName {1} and Type {2}",
                            keyTTY, ttyCollection.GetTTYElement(keyTTY).entityName,
                                Type.GetType(ttyCollection.GetTTYElement(keyTTY).entityName)), this);

                    messageObject = (IBaggageTTYTable)Activator.CreateInstance(Type.GetType(ttyCollection.GetTTYElement(keyTTY).entityName));
                    messageObject.Clean();

                    foreach (string element in elements)
                    {
                        components = Regex.Split(element, matchComponents);

                        keyElement = (!element.Equals(elements[0])) ? components[0] : "default";

                        BagTMLog.LogDebug(
                            String.Format("BagTM Queue Message Parsing TTY Message with Key {0} Processing KeyElement {1} for Element {2} and Components[0] {3} ",
                               keyTTY, keyElement, element, components[0]), this);
                        
                        if (components[0] != null && ttyCollection.HasTTYElement(keyTTY, keyElement))
                        {

                            foreach (TTYComponentConfigElement config in ttyCollection.GetTTYElement(keyTTY).ttyElements.GetTTYElementElement(keyElement).ttycomponents)
                            {
                                BagTMLog.LogDebug(
                                    String.Format("BagTM Queue Message Parsing TTY Message with Config Size {0}", ttyCollection.GetTTYElement(keyTTY).ttyElements.GetTTYElementElement(keyElement).ttycomponents.Count), this);

                                BagTMLog.LogDebug(
                                    String.Format("BagTM Queue Message Parsing TTY Message key {0}, keyElement {1}, position {2} and substring {3} from components {4}",
                                        keyTTY, keyElement, config.GetPositionInt(), config.substring, components.Length), this);
                                
                                parameter = null;
                                methodName = config.methodName;

                                // To process all components based on XML configuration
                                if (config.GetPositionInt() < 0)
                                {
                                    // If position = 0 then all components in the same attribute
                                    // Also remove . and /
                                    parameter = element.Substring(components[0].Length + 1);

                                }
                                else if (config.GetPositionInt() < components.Length)
                                {
                                    if (config.substring != null && config.substring.Length > 0)
                                    {
                                        if (components[config.GetPositionInt()].Length >= config.StartSubString() + config.LenghtSubString())
                                        {
                                            parameter = components[config.GetPositionInt()].
                                                Substring(config.StartSubString(), config.LenghtSubString());
                                        } else
                                        {
                                            parameter = null;
                                        }
                                    } else
                                    {
                                        parameter = components[config.GetPositionInt()];
                                    }
                                }

                                if (config.GetPositionInt() < components.Length)
                                {
                                    BagTMLog.LogDebug(
                                        String.Format("BagTM Queue Message Parsing TTY Message key {0} and keyElement {1} position {2} to method {3} with parameters {4}",
                                            keyTTY, keyElement, config.position, methodName, parameter), this);
                                    
                                    // Not necessary if parameter is null since no change to object
                                    if (parameter != null) messageObject.SetValue(methodName, parameter);

                                    BagTMLog.LogDebug(
                                        String.Format("BagTM Queue Message Parsing TTY Message key {0} and keyElement {1} position {2} stored object {3}",
                                            keyTTY, keyElement, config.position, messageObject.ToString()), this);
                                }

                            } 
                        }
                        else
                        {
                            BagTMLog.LogDebug(
                                String.Format("BagTM Queue Message Parsing TTY Message key {0} and keyElement {1} does not process in element {2}",
                                    keyTTY, keyElement, element), this);
                        }
                    }
                   
                }
                else
                {
                    BagTMLog.LogDebug(
                        String.Format("BagTM Queue Message Parsing TTY Message key {0} does not process {1}", elements[0], messageString), this);
                    
                }
            } catch (Exception e)
            {
                BagTMLog.LogError("BagTM Queue Message Parsing TTY Parser Error", this, e);
                
                throw e;
            }

            BagTMLog.LogDebug(
                        String.Format("BagTM Queue Message Parsing TTY Parser Collection {0}", messageString), this);

            // Se o objeto estiver instanciado então foi preenchido e deve ser guardado em base de dados
            if (messageObject != null)
            {
                BagTMLog.LogInfo(
                        String.Format("BagTM Queue Message Message Parsed {0}", messageObject), this);
                return messageObject.Save(db);
            }
            else return null;
        }

        private String RemoveHeaders (String message)
        {
            int indexOfSMI = 99999;

            foreach (TTYConfigElement config in ttyCollection)
            {
                if (message.IndexOf(config.key) > -1)
                    indexOfSMI = Math.Min(indexOfSMI, message.IndexOf(config.key));
            }
                
            message = (indexOfSMI != 99999 && indexOfSMI > 1) ? message.Remove(0, indexOfSMI) : message;

            return message.Replace("=TEXT", "").Replace("ENDBSM", "").Replace("ENDBPM", "");

        }
    }
}
