using System;
using System.Threading.Tasks;

namespace DriveSync.Shared
{
    public class SyncRunner
    {
        private readonly SyncSettings _settings;
        private readonly EmailNotifier _emailNotifier;

        public SyncRunner(SyncSettings settings)
        {
            _settings = settings;
            _emailNotifier = new EmailNotifier(settings);
        }

        public bool IsDueNow(DateTime now)
        {
            if (!_settings.Enabled)
                return false;

            if (!_settings.SyncDays.Contains(now.DayOfWeek))
                return false;

            var current = new TimeSpan(now.Hour, now.Minute, 0);

            foreach (var scheduled in _settings.SyncTimes)
            {
                var scheduledTime = new TimeSpan(scheduled.Hours, scheduled.Minutes, 0);
                if (current == scheduledTime)
                    return true;
            }

            return false;
        }

        public async Task RunOnceAsync()
        {
            Logger.Info("Sync started.");

            if (!_settings.Enabled)
                return;

            if (string.IsNullOrWhiteSpace(_settings.GoogleDriveOAuthClientSecretJsonPath))
                throw new InvalidOperationException("OAuth client secret JSON path is required.");

            try
            {
                var driveService = await GoogleDriveService.CreateOAuthDriveServiceAsync(_settings.GoogleDriveOAuthClientSecretJsonPath);
                var googleDrive = new GoogleDriveService(driveService);

                if (string.IsNullOrWhiteSpace(_settings.GoogleDriveFolderId))
                {
                    var createdId = await googleDrive.CreateFolderAsync(_settings.GoogleDriveFolderName);
                    _settings.GoogleDriveFolderId = createdId;
                    SettingsStore.Save(_settings);
                }
                else
                {
                    await googleDrive.EnsureDestinationFolderAsync(_settings.GoogleDriveFolderId, _settings.GoogleDriveFolderName);
                }

                foreach (var path in _settings.SelectedPaths)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
                    {
                        Logger.Error("Skipped missing path: " + path);
                        continue;
                    }

                    await googleDrive.UploadPathAsync(path, _settings.GoogleDriveFolderId, _settings.IncludeSubFolders);
                    Logger.Info("Uploaded: " + path);
                }

                if (_settings.NotifyOnSuccess)
                    await _emailNotifier.SendAsync("DriveSync success", "Sync completed successfully at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                Logger.Info("Sync completed.");
            }
            catch (Exception ex)
            {
                Logger.Error("Sync failed: " + ex.ToString());

                if (_settings.NotifyOnError)
                    await _emailNotifier.SendAsync("DriveSync error", ex.ToString());

                throw;
            }
        }
    }
}
