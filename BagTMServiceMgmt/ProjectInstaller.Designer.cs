namespace BagTMServiceMgmt
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcess = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstallerEngine = new System.ServiceProcess.ServiceInstaller();
            this.serviceInstallerQueue = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcess
            // 
            this.serviceProcess.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcess.Password = null;
            this.serviceProcess.Username = null;
            this.serviceProcess.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_AfterInstall);
            // 
            // serviceInstallerEngine
            // 
            this.serviceInstallerEngine.DisplayName = "BagTM Engine Service";
            this.serviceInstallerEngine.ServiceName = "BagTMEngineServiceMgmt";
            // 
            // serviceInstallerQueue
            // 
            this.serviceInstallerQueue.DisplayName = "BagTM Queue Service";
            this.serviceInstallerQueue.ServiceName = "BagTMQueueServiceMgmt";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcess,
            this.serviceInstallerQueue,
            this.serviceInstallerEngine});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcess;
        private System.ServiceProcess.ServiceInstaller serviceInstallerEngine;
        private System.ServiceProcess.ServiceInstaller serviceInstallerQueue;
    }
}