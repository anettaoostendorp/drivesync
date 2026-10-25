using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DriveSync.Service
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private readonly ServiceProcessInstaller _processInstaller;
        private readonly ServiceInstaller _serviceInstaller;

        public ProjectInstaller()
        {
            _processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            _serviceInstaller = new ServiceInstaller
            {
                ServiceName = "DriveSyncService",
                DisplayName = "DriveSync Service",
                Description = "Sync selected files/folders to Google Drive",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(_processInstaller);
            Installers.Add(_serviceInstaller);
        }
    }
}
