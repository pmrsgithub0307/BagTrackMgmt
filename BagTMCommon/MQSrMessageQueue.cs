using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBM.WMQ;
using System.Collections;
using IBM.XMS;

namespace BagTMCommon
{
    /// <summary>
    /// Author: Reza Servia
    /// Name: MQTest.cs
    /// Description:
    /// MQ Manager Class
    /// Put/Get Messages To/From Local Queue 
    /// </summary>
    public class MQSrMessageQueue
    {
        public MQSrMessageQueue()
        {
            
            XMSFactoryFactory xff = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ);
            IConnectionFactory cf = xff.CreateConnectionFactory();
            cf.SetStringProperty(XMSC.WMQ_HOST_NAME, "ICEKOPAS18");
            cf.SetIntProperty(XMSC.WMQ_PORT, 1414);
            cf.SetStringProperty(XMSC.WMQ_CHANNEL, "SYSTEM.DEF.SVRCONN");
            cf.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER, "FI.CADS");
            cf.SetIntProperty(XMSC.WMQ_CONNECTION_MODE, XMSC.WMQ_CM_DIRECT_HTTP);
            cf.SetIntProperty(XMSC.WMQ_BROKER_VERSION, XMSC.WMQ_BROKER_V2);
            
            //cf.SetStringProperty(XMSC.USERID, "mqm");
            //cf.SetStringProperty(XMSC.PASSWORD, "mqm");

            IConnection conn = cf.CreateConnection();
            Console.WriteLine("connection created");
            ISession sess = conn.CreateSession(false, AcknowledgeMode.AutoAcknowledge);
            //IDestination dest = sess.CreateQueue("DOX.APYMT.ESB.SSK.RPO.02");
            //IMessageConsumer consumer = sess.CreateConsumer(dest);
            //MessageListener ml = new MessageListener(OnMessage);
            //consumer.MessageListener = ml;
            conn.Start();
            Console.WriteLine("Consumer started");


        }

        private void OnMessage(IMessage msg)
        {
            ITextMessage textMsg = (ITextMessage)msg;
            Console.Write("Got a message: ");
            Console.WriteLine(textMsg.Text);
        }
    }
}
