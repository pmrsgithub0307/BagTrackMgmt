using BagTMEngineProcessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BagTMServiceMgmt
{
    partial class BagTMEngineServiceMgmt : ServiceBase
    {
        private IExecutionEngineProcessing engineProcess;

        public BagTMEngineServiceMgmt()
        {
            InitializeComponent();

            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;
        }

        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new BagTMEngineServiceMgmt()
            };
            ServiceBase.Run(ServicesToRun);
        }

        /// <summary>
        /// OnStart(): Put startup code here
        ///  - Start threads, get inital data, etc.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.  
            ServiceStatus serviceStatus = new ServiceStatus();

            try
            {
                BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Start", this);

                // Set up a timer to trigger every minute.  
                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 500; // 10 seconds  
                timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
                timer.Start();

                serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
                serviceStatus.dwWaitHint = 100000;
                SetServiceStatus(this.ServiceHandle, ref serviceStatus);

                engineProcess = new ExecutionDataflowEngineProcessing();

                base.OnStart(args);
                
                // Update the service state to Running.  
                serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
                SetServiceStatus(this.ServiceHandle, ref serviceStatus);

                BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Start End", this);
            }
            catch (Exception e)
            {
                // Update the service state to Running.  
                serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
                SetServiceStatus(this.ServiceHandle, ref serviceStatus);

                BagTMCommon.BagTMLog.LogError("BagTM Engine On Start Error", this, e);
            }
        }

        /// <summary>
        /// OnStop(): Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Stop", this);

            base.OnStop();

            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Stop End", this);
        }

        /// <summary>
        /// OnPause: Put your pause code here
        /// - Pause working threads, etc.
        /// </summary>
        protected override void OnPause()
        {
            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Pause", this);

            base.OnPause();

            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Pause End", this);
        }

        /// <summary>
        /// OnContinue(): Put your continue code here
        /// - Un-pause working threads, etc.
        /// </summary>
        protected override void OnContinue()
        {
            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Continue", this);

            base.OnContinue();

            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Continue End", this);
        }

        /// <summary>
        /// OnShutdown(): Called when the System is shutting down
        /// - Put code here when you need special handling
        ///   of code that deals with a system shutdown, such
        ///   as saving special data before shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Shutdown", this);

            base.OnShutdown();

            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Shutdown", this);
        }

        /// <summary>
        /// OnCustomCommand(): If you need to send a command to your
        ///   service without the need for Remoting or Sockets, use
        ///   this method to do custom methods.
        /// </summary>
        /// <param name="command">Arbitrary Integer between 128 & 256</param>
        protected override void OnCustomCommand(int command)
        {
            //  A custom command can be sent to a service by using this method:
            //#  int command = 128; //Some Arbitrary number between 128 & 256
            //#  ServiceController sc = new ServiceController("NameOfService");
            //#  sc.ExecuteCommand(command);
            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Command", this);

            base.OnCustomCommand(command);

            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Command", this);
        }

        /// <summary>
        /// OnPowerEvent(): Useful for detecting power status changes,
        ///   such as going into Suspend mode or Low Battery for laptops.
        /// </summary>
        /// <param name="powerStatus">The Power Broadcast Status
        /// (BatteryLow, Suspend, etc.)</param>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On PowerEvent", this);
            
            return base.OnPowerEvent(powerStatus);

        }

        /// <summary>
        /// OnSessionChange(): To handle a change event
        ///   from a Terminal Server session.
        ///   Useful if you need to determine
        ///   when a user logs in remotely or logs off,
        ///   or when someone logs into the console.
        /// </summary>
        /// <param name="changeDescription">The Session Change
        /// Event that occured.</param>
        protected override void OnSessionChange(
                    SessionChangeDescription changeDescription)
        {
            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Session Change", this);

            base.OnSessionChange(changeDescription);

            BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Session Change End", this);

        }

        /// <summary>
        /// Handle timer for OnStart() - Test from MCDN
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            try
            {
                BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Timer", this);

                BagTMCommon.BagTMLog.LogDebug("Entering Bag Track Mgmt Entering EngineProcessing", this);
                engineProcess.EngineProcessing();
                BagTMCommon.BagTMLog.LogDebug("Entering Bag Track Mgmt Finnish EngineProcessing", this);
                BagTMCommon.BagTMLog.LogDebug("BagTM Engine On Timer", this);
            }
            catch (Exception e)
            {
                BagTMCommon.BagTMLog.LogError("BagTM Engine On Timer Error", this, e);
            }
        }
    }
}
