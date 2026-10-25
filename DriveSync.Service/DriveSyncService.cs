using System;
using System.ServiceProcess;
using System.Threading;
using DriveSync.Shared;

namespace DriveSync.Service
{
    public class DriveSyncWindowsService : ServiceBase
    {
        private Timer _timer;

        public DriveSyncWindowsService()
        {
            ServiceName = "DriveSyncService";
            CanStop = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            Logger.Info("DriveSync service started.");
            _timer = new Timer(CheckSchedule, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        protected override void OnStop()
        {
            Logger.Info("DriveSync service stopped.");
            _timer?.Dispose();
        }

        private void CheckSchedule(object state)
        {
            try
            {
                var settings = SettingsStore.Load();
                var runner = new SyncRunner(settings);

                if (runner.IsDueNow(DateTime.Now))
                    runner.RunOnceAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error("Service sync failed: " + ex.Message);
            }
        }
    }
}
