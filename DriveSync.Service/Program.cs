using System.ServiceProcess;

namespace DriveSync.Service
{
    static class Program
    {
        static void Main()
        {
            ServiceBase.Run(new DriveSyncWindowsService());
        }
    }
}
