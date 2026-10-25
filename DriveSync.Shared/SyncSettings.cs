using System;
using System.Collections.Generic;

namespace DriveSync.Shared
{
    public class SyncSettings
    {
        public bool Enabled { get; set; } = true;
        public bool UseGoogleDriveOAuth { get; set; } = true;
        public string GoogleDriveOAuthClientSecretJsonPath { get; set; } = "";
        public string GoogleDriveServiceAccountJsonPath { get; set; } = "";
        public string GoogleDriveFolderId { get; set; } = "";
        public string GoogleDriveFolderName { get; set; } = "DriveSync";

        public List<string> SelectedPaths { get; set; } = new List<string>();
        public bool IncludeSubFolders { get; set; } = true;
        public bool SyncFiles { get; set; } = true;
        public bool SyncFolders { get; set; } = true;

        public List<DayOfWeek> SyncDays { get; set; } = new List<DayOfWeek>
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };

        public List<TimeSpan> SyncTimes { get; set; } = new List<TimeSpan>
        {
            new TimeSpan(2, 0, 0),
            new TimeSpan(18, 0, 0)
        };

        public bool NotifyOnSuccess { get; set; } = true;
        public bool NotifyOnError { get; set; } = true;
        public string EmailFrom { get; set; } = "";
        public string EmailTo { get; set; } = "";
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public bool SmtpUseSsl { get; set; } = true;
    }
}
