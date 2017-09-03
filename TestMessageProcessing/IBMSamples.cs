/*********************************************************************/
/*                                                                   */
/*                  IBM Message Service Client for .NET              */
/*                                                                   */
/*  FILE NAME:      SimpleConsumer.cs                                */
/*                                                                   */
/*  DESCRIPTION:    Basic example of a simple message consumer       */
/*                                                                   */
/*********************************************************************/
/*  <START_COPYRIGHT>                                                */
/*                                                                   */
/*  Licensed Materials - Property of IBM                             */
/*                                                                   */
/*  5724-M21                                                         */
/*                                                                   */
/*  (C) COPYRIGHT International Business Machines Corp. 2003, 2010   */
/*  All Rights Reserved                                              */
/*                                                                   */
/*  U.S. Government Users Restricted Rights - use, duplication or    */
/*  disclosure restricted by GSA ADP Schedule Contract with IBM Corp.*/
/*                                                                   */
/*  Status: Version 2 Release 0                                      */
/*  <END_COPYRIGHT>                                                  */
/*                                                                   */
/*********************************************************************/
/* A simple application demonstrate synchronous message consumer.    */
/*                                                                   */
/* Notes: The consumer receives messages synchronously.              */
/*                                                                   */
/* API type: IBM XMS.NET API v2.0                                    */
/*                                                                   */
/* Messaging domain: P2P and Pub/Sub                                 */
/*                                                                   */
/* Provider type: WebSphere MQ                                       */
/*                                                                   */
/* JNDI in use: No                                                   */
/*                                                                   */
/*********************************************************************/

using System;
using IBM.XMS;

namespace TestMessageProcessing
{
    class SimpleConsumer
    {
        /// <summary>
        /// Name of the host on which WMQ Queue manager is running 
        /// </summary>
        private String hostName = "ICEKOPAS18";

        /// <summary>
        /// Port number on which WMQ Queue manager is listening
        /// </summary>
        private int port = 1414;

        /// <summary>
        /// Name of the channel
        /// </summary>
        private String channelName = "SYSTEM.DEF.SVRCONN";

        /// <summary>
        /// Name of the WMQ Queue manager to connect to
        /// </summary>
        private String queueManagerName;

        /// <summary>
        /// Queue or Topic name.
        /// </summary>
        private String destinationName;

        /// <summary>
        /// Is destination of topic type
        /// </summary>
        private bool isTopic = false;

        /// <summary>
        /// Timeout
        /// </summary>
        private const int TIMEOUTTIME = 30000;

        /// <summary>
        /// Key repository
        /// </summary>
        private String sslKeyRepository = null;
        /// <summary>
        /// CipherSpec name.
        /// </summary>
        private String cipherSpec = null;
        /// <summary>
        /// sslPeerName
        /// </summary>
        private String sslPeerName = null;
        /// <summary>
        /// KeyResetCount value.
        /// </summary>
        private int keyResetCount = -1;
        /// <summary>
        /// SSLCertRevocationCheck
        /// </summary>
        private Boolean sslCertRevocationCheck = false;

        /// <param name="args"></param>
        static void MainDisabled(string[] args)
        {
            Console.WriteLine("===> START of Simple Consumer sample for WMQ transport <===\n\n");
            try
            {
                SimpleConsumer simpleConsumer = new SimpleConsumer();
                if (simpleConsumer.ParseCommandline(args))
                    simpleConsumer.ReceiveMessages();
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Invalid arguments!\n{0}", ex);
            }
            catch (XMSException ex)
            {
                Console.WriteLine("XMSException caught: {0}", ex);
                if (ex.LinkedException != null)
                {
                    Console.WriteLine("Stack Trace:\n {0}", ex.LinkedException.StackTrace);
                }
                Console.WriteLine("Sample execution  FAILED!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught: {0}", ex);
                Console.WriteLine("Sample execution  FAILED!");
            }
            Console.WriteLine("===> END of Simple Consumer sample for WMQ transport <===\n\n");
        }

        /// <summary>
        /// Method to connect to queue manager and receive messages.
        /// </summary>
        void ReceiveMessages()
        {
            XMSFactoryFactory factoryFactory;
            IConnectionFactory cf;
            IConnection connectionWMQ;
            ISession sessionWMQ;
            IDestination destination;
            IMessageConsumer consumer;
            ITextMessage textMessage;

            // Get an instance of factory.
            factoryFactory = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ);

            // Create WMQ Connection Factory.
            cf = factoryFactory.CreateConnectionFactory();
            Console.WriteLine("Connection Factory created.");

            // Set the properties
            cf.SetStringProperty(XMSC.WMQ_HOST_NAME, hostName);
            cf.SetIntProperty(XMSC.WMQ_PORT, port);
            cf.SetStringProperty(XMSC.WMQ_CHANNEL, channelName);
            cf.SetIntProperty(XMSC.WMQ_CONNECTION_MODE, XMSC.WMQ_CM_CLIENT);
            cf.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER, queueManagerName);
            if (sslKeyRepository != null)
            {
                cf.SetStringProperty(XMSC.WMQ_SSL_KEY_REPOSITORY, "sslKeyRepository");
            }
            if (cipherSpec != null)
            {
                cf.SetStringProperty(XMSC.WMQ_SSL_CIPHER_SPEC, cipherSpec);
            }
            if (sslPeerName != null)
            {
                cf.SetStringProperty(XMSC.WMQ_SSL_PEER_NAME, sslPeerName);
            }
            if (keyResetCount != -1)
            {
                cf.SetIntProperty(XMSC.WMQ_SSL_KEY_RESETCOUNT, keyResetCount);
            }
            if (sslCertRevocationCheck != false)
            {
                cf.SetBooleanProperty(XMSC.WMQ_SSL_CERT_REVOCATION_CHECK, true);
            }
            cf.SetStringProperty(XMSC.USERID, "bagmon");
            cf.SetStringProperty(XMSC.PASSWORD, "Iceair123");

            // Create connection.
            connectionWMQ = cf.CreateConnection();
            Console.WriteLine("Connection created.");

            // Create session
            sessionWMQ = connectionWMQ.CreateSession(false, AcknowledgeMode.AutoAcknowledge);
            Console.WriteLine("Session created.");

            // Create destination
            if (isTopic)
                destination = sessionWMQ.CreateTopic(destinationName);
            else
                destination = sessionWMQ.CreateQueue(destinationName);
            Console.WriteLine("Destination created.");

            // Create consumer
            consumer = sessionWMQ.CreateConsumer(destination);
            Console.WriteLine("Message Consumer created. Starting the connection now.");
            // Start the connection to receive messages.
            connectionWMQ.Start();

            Console.WriteLine("Receive message: " + TIMEOUTTIME / 1000 + " seconds wait time");
            // Wait for 30 seconds for messages. Exit if no message by then
            textMessage = (ITextMessage)consumer.Receive(TIMEOUTTIME);
            if (textMessage != null)
            {
                Console.WriteLine("Message received.");
                Console.Write(textMessage);
                Console.WriteLine("\n");
            }
            else
                Console.WriteLine("Wait timed out.");

            // Cleanup
            consumer.Close();
            destination.Dispose();
            sessionWMQ.Dispose();
            connectionWMQ.Close();
        }

        /// <summary>
        /// Parse commandline parameters
        /// Usage: SimpleConsumer -m queueManager -d destinationURI [-h host -p port -l channel]
        /// </summary>
        /// <param name="args"></param>
        bool ParseCommandline(string[] args)
        {
            String token;

            if (args.Length < 4)
            {
                Console.WriteLine("Usage: SimpleConsumer -m queueManager -d destinationURI -k keyrespository [-h host -p port -l channel -s cipherspec -dn sslpeername -kr keyresetcount -cr sslcertificate revocation check]");
                Console.WriteLine("Ex: SimpleConsumer -m QM -d QA");
                Console.WriteLine("    SimpleConsumer -m QM -d topic://TopicA -h remotehost -p 1414 -l SYSTEM.DEF.SVRCONN");
                Console.WriteLine("For Ssl Connections: SimpleConsumer -m QM -d QA -k *SYSTEM");
                Console.WriteLine("                     SimpleConsumer -m QM -d QA -k *SYSTEM -s TLS_RSA_WITH_AES_128_CBC_SHA256 -kr 45000");
                return false;
            }

            for (int argIndex = 0; argIndex < args.Length; argIndex++)
            {
                // Get the token
                token = (String)args.GetValue(argIndex);

                // Queue manager name
                if (token == "-m")
                {
                    // Next parameter is queue manager name
                    queueManagerName = (String)args.GetValue(argIndex + 1);
                    argIndex++;
                    continue;
                }
                else if (token == "-d")
                {
                    // destination - queue or topic
                    String destination;
                    // Next parameter will either be queue name or topic string
                    destination = (String)args.GetValue(argIndex + 1);
                    if (destination.StartsWith("topic://"))
                    {
                        isTopic = true;
                        destination = destination.Remove(0, 8);
                        destinationName = destination;
                    }
                    else
                    {
                        // Does not start with topic, then it's a queue name.
                        if (destination.StartsWith("queue://"))
                            destination = destination.Remove(0, 8);
                        destinationName = destination;
                    }
                    argIndex++;
                    continue;
                }
                else if (token == "-h")
                {
                    // host name
                    hostName = (String)args.GetValue(argIndex + 1);
                    argIndex++;
                    continue;
                }
                else if (token == "-p")
                {
                    // port name
                    port = Convert.ToInt32((String)args.GetValue(argIndex + 1));
                    argIndex++;
                    continue;
                }
                else if (token == "-l")
                {
                    // port name
                    channelName = (String)args.GetValue(argIndex + 1);
                    argIndex++;
                    continue;
                }
                else if (token == "-k")
                {
                    sslKeyRepository = (String)args.GetValue(argIndex + 1);
                    argIndex++;
                }
                else if (token == "-s")
                {
                    cipherSpec = (String)args.GetValue(argIndex + 1);
                    argIndex++;
                }
                else if (token == "-dn")
                {
                    sslPeerName = (String)args.GetValue(argIndex + 1);
                    argIndex++;
                }
                else if (token == "-kr")
                {
                    keyResetCount = Convert.ToInt32((String)args.GetValue(argIndex + 1));
                    argIndex++;
                }
                else if (token == "-cr")
                {
                    sslCertRevocationCheck = Convert.ToBoolean((String)args.GetValue(argIndex + 1));
                    argIndex++;
                }
                else
                {
                    throw new ArgumentException("Invalid argument", token);
                }
            }
            return true;
        }
    }
}
