using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace DriveSync.Shared
{
    public class GoogleDriveService
    {
        private readonly DriveService _driveService;

        public GoogleDriveService(DriveService driveService)
        {
            _driveService = driveService;
        }

        public static async Task<DriveService> CreateOAuthDriveServiceAsync(string clientSecretJsonPath)
        {
            if (string.IsNullOrWhiteSpace(clientSecretJsonPath))
                throw new InvalidOperationException("Google OAuth client secret JSON path is required.");

            var secrets = GoogleClientSecrets.FromFile(clientSecretJsonPath);
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets.Secrets,
                new[] { DriveService.Scope.Drive },
                "user",
                CancellationToken.None,
                new FileDataStore("DriveSyncGoogleAuth", true));

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "DriveSync"
            });
        }

        public async Task<List<File>> GetFoldersAsync()
        {
            var request = _driveService.Files.List();
            request.Q = "mimeType='application/vnd.google-apps.folder' and trashed=false";
            request.Fields = "files(id,name)";
            request.PageSize = 1000;

            var result = await request.ExecuteAsync();
            return result.Files != null ? result.Files.ToList() : new List<File>();
        }

        public async Task<bool> FolderExistsAsync(string folderId)
        {
            if (string.IsNullOrWhiteSpace(folderId))
                return false;

            try
            {
                var request = _driveService.Files.Get(folderId);
                request.Fields = "id,name";
                await request.ExecuteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> CreateFolderAsync(string folderName)
        {
            var safeName = string.IsNullOrWhiteSpace(folderName) ? "DriveSync" : folderName;
            var folder = new File { Name = safeName, MimeType = "application/vnd.google-apps.folder" };
            var request = _driveService.Files.Create(folder);
            request.Fields = "id,name";
            var result = await request.ExecuteAsync();
            return result != null ? result.Id : "";
        }

        public async Task EnsureDestinationFolderAsync(string folderId, string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderId))
            {
                var created = await CreateFolderAsync(folderName);
                if (string.IsNullOrWhiteSpace(created))
                    throw new InvalidOperationException("Could not create destination folder.");
                return;
            }

            var exists = await FolderExistsAsync(folderId);
            if (!exists)
            {
                var created = await CreateFolderAsync(folderName);
                if (string.IsNullOrWhiteSpace(created))
                    throw new InvalidOperationException("Destination folder does not exist and could not be recreated.");
            }
        }

        public async Task UploadPathAsync(string localPath, string destinationFolderId, bool includeSubFolders)
        {
            if (File.Exists(localPath))
            {
                await UploadSingleFileAsync(localPath, destinationFolderId);
                return;
            }

            if (Directory.Exists(localPath))
            {
                await UploadDirectoryAsync(localPath, destinationFolderId, includeSubFolders);
                return;
            }

            throw new FileNotFoundException("Path not found: " + localPath);
        }

        private async Task UploadDirectoryAsync(string localDirectory, string parentFolderId, bool includeSubFolders)
        {
            var folderName = Path.GetFileName(localDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var targetFolderId = await EnsureFolderExistsInParentAsync(folderName, parentFolderId);

            foreach (var file in Directory.GetFiles(localDirectory))
                await UploadSingleFileAsync(file, targetFolderId);

            if (includeSubFolders)
            {
                foreach (var childDirectory in Directory.GetDirectories(localDirectory))
                    await UploadDirectoryAsync(childDirectory, targetFolderId, true);
            }
        }

        private async Task<string> EnsureFolderExistsInParentAsync(string folderName, string parentFolderId)
        {
            var query = "mimeType='application/vnd.google-apps.folder' and name='" + EscapeQuery(folderName) + "' and trashed=false";
            if (!string.IsNullOrWhiteSpace(parentFolderId))
                query += " and '" + parentFolderId + "' in parents";

            var request = _driveService.Files.List();
            request.Q = query;
            request.Fields = "files(id,name)";
            var result = await request.ExecuteAsync();

            if (result.Files != null && result.Files.Count > 0)
                return result.Files[0].Id;

            var folder = new File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = string.IsNullOrWhiteSpace(parentFolderId) ? null : new[] { parentFolderId }
            };

            var createRequest = _driveService.Files.Create(folder);
            createRequest.Fields = "id";
            var created = await createRequest.ExecuteAsync();
            return created != null ? created.Id : "";
        }

        private async Task UploadSingleFileAsync(string localFilePath, string parentFolderId)
        {
            var targetName = Path.GetFileName(localFilePath);
            var existingId = await FindFileByNameAsync(targetName, parentFolderId);

            using (var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                var metadata = new File
                {
                    Name = targetName,
                    Parents = string.IsNullOrWhiteSpace(parentFolderId) ? null : new[] { parentFolderId }
                };

                if (!string.IsNullOrWhiteSpace(existingId))
                {
                    var updateRequest = _driveService.Files.Update(metadata, existingId);
                    updateRequest.Fields = "id";
                    await updateRequest.UploadAsync();
                    return;
                }

                var createRequest = _driveService.Files.Create(metadata, stream, GetMimeType(localFilePath));
                createRequest.Fields = "id";
                await createRequest.UploadAsync();
            }
        }

        private async Task<string> FindFileByNameAsync(string fileName, string parentFolderId)
        {
            var query = "name='" + EscapeQuery(fileName) + "' and trashed=false";
            if (!string.IsNullOrWhiteSpace(parentFolderId))
                query += " and '" + parentFolderId + "' in parents";

            var request = _driveService.Files.List();
            request.Q = query;
            request.Fields = "files(id,name)";
            var result = await request.ExecuteAsync();

            return result.Files != null && result.Files.Count > 0 ? result.Files[0].Id : "";
        }

        private static string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            switch (ext)
            {
                case ".txt": return "text/plain";
                case ".csv": return "text/csv";
                case ".json": return "application/json";
                case ".xml": return "application/xml";
                case ".pdf": return "application/pdf";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".zip": return "application/zip";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                default: return "application/octet-stream";
            }
        }

        private static string EscapeQuery(string text)
        {
            return text.Replace("\\", "\\\\").Replace("'", "\\'");
        }
    }
}
