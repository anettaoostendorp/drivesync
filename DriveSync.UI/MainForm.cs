using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DriveSync.Shared;

namespace DriveSync.UI
{
    public partial class MainForm : Form
    {
        private readonly SyncSettings _settings;
        private readonly List<string> _selectedPaths = new List<string>();
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private readonly Timer _backgroundTimer = new Timer();

        private TextBox txtOAuthClientSecretPath;
        private TextBox txtDriveFolderId;
        private TextBox txtDriveFolderName;
        private ListBox lbPaths;
        private CheckedListBox clbDays;
        private ListBox lbTimes;
        private CheckBox chkFiles;
        private CheckBox chkFolders;
        private CheckBox chkSubfolders;
        private CheckBox chkNotifySuccess;
        private CheckBox chkNotifyError;
        private CheckBox chkUseSsl;
        private TextBox txtSmtpHost;
        private TextBox txtSmtpUser;
        private TextBox txtSmtpPassword;
        private TextBox txtSmtpFrom;
        private TextBox txtSmtpTo;
        private NumericUpDown nudSmtpPort;

        public MainForm(SyncSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            LoadSettingsIntoForm();

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", (_, __) => ShowWindow());
            menu.Items.Add("Run sync now", async (_, __) => await RunSyncNowAsync());
            menu.Items.Add("Exit", (_, __) => CloseApplication());

            _notifyIcon.Icon = SystemIcons.Information;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "DriveSync";
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (_, __) => ShowWindow();

            _backgroundTimer.Interval = 60000;
            _backgroundTimer.Tick += async (_, __) => await CheckForScheduledSyncAsync();
            _backgroundTimer.Start();

            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Visible = false;
        }

        private void InitializeComponent()
        {
            Text = "DriveSync";
            Width = 980;
            Height = 760;
            StartPosition = FormStartPosition.CenterScreen;
            FormClosing += MainForm_FormClosing;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            var tabGeneral = new TabPage("General");
            var tabSchedule = new TabPage("Schedule");
            var tabEmail = new TabPage("Email");

            var generalLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 12 };
            generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            var lblOAuth = new Label { Text = "OAuth client secret JSON:" };
            txtOAuthClientSecretPath = new TextBox();
            var btnBrowseOAuth = new Button { Text = "Browse" };

            var lblFolderId = new Label { Text = "Drive folder ID:" };
            txtDriveFolderId = new TextBox();

            var lblFolderName = new Label { Text = "Folder name:" };
            txtDriveFolderName = new TextBox();

            var btnSelectGoogleFolder = new Button { Text = "Select Google Drive folder" };

            var lblPaths = new Label { Text = "Files and folders to sync:" };
            lbPaths = new ListBox { Height = 180 };
            var btnAddFile = new Button { Text = "Add file" };
            var btnAddFolder = new Button { Text = "Add folder" };
            var btnRemovePath = new Button { Text = "Remove" };

            chkFiles = new CheckBox { Text = "Sync files", Checked = true };
            chkFolders = new CheckBox { Text = "Sync folders", Checked = true };
            chkSubfolders = new CheckBox { Text = "Include subfolders", Checked = true };

            generalLayout.Controls.Add(lblOAuth, 0, 0);
            var oauthPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            oauthPanel.Controls.Add(txtOAuthClientSecretPath);
            oauthPanel.Controls.Add(btnBrowseOAuth);
            generalLayout.Controls.Add(oauthPanel, 1, 0);

            generalLayout.Controls.Add(lblFolderId, 0, 1);
            generalLayout.Controls.Add(txtDriveFolderId, 1, 1);
            generalLayout.Controls.Add(lblFolderName, 0, 2);
            generalLayout.Controls.Add(txtDriveFolderName, 1, 2);

            var folderButtons = new FlowLayoutPanel { AutoSize = true };
            folderButtons.Controls.Add(btnSelectGoogleFolder);
            generalLayout.Controls.Add(folderButtons, 1, 3);

            generalLayout.Controls.Add(lblPaths, 0, 4);
            var pathsTable = new TableLayoutPanel { AutoSize = true, ColumnCount = 1 };
            pathsTable.Controls.Add(lbPaths);
            var pathButtons = new FlowLayoutPanel { AutoSize = true };
            pathButtons.Controls.Add(btnAddFile);
            pathButtons.Controls.Add(btnAddFolder);
            pathButtons.Controls.Add(btnRemovePath);
            pathsTable.Controls.Add(pathButtons);
            generalLayout.Controls.Add(pathsTable, 1, 4);

            generalLayout.Controls.Add(chkFiles, 1, 5);
            generalLayout.Controls.Add(chkFolders, 1, 6);
            generalLayout.Controls.Add(chkSubfolders, 1, 7);

            tabGeneral.Controls.Add(generalLayout);

            var scheduleLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 4 };
            scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

            var lblDays = new Label { Text = "Sync days:" };
            clbDays = new CheckedListBox();
            clbDays.Items.Add("Monday");
            clbDays.Items.Add("Tuesday");
            clbDays.Items.Add("Wednesday");
            clbDays.Items.Add("Thursday");
            clbDays.Items.Add("Friday");
            clbDays.Items.Add("Saturday");
            clbDays.Items.Add("Sunday");

            var lblTimes = new Label { Text = "Sync times:" };
            lbTimes = new ListBox { Height = 180 };
            var btnAddTime = new Button { Text = "Add time" };
            var btnRemoveTime = new Button { Text = "Remove time" };

            scheduleLayout.Controls.Add(lblDays, 0, 0);
            scheduleLayout.Controls.Add(clbDays, 1, 0);

            var timeTable = new TableLayoutPanel { AutoSize = true, ColumnCount = 1 };
            timeTable.Controls.Add(lbTimes);
            var timeButtons = new FlowLayoutPanel { AutoSize = true };
            timeButtons.Controls.Add(btnAddTime);
            timeButtons.Controls.Add(btnRemoveTime);
            timeTable.Controls.Add(timeButtons);

            scheduleLayout.Controls.Add(lblTimes, 0, 1);
            scheduleLayout.Controls.Add(timeTable, 1, 1);
            tabSchedule.Controls.Add(scheduleLayout);

            var emailLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 10 };
            emailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            emailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

            var lblHost = new Label { Text = "SMTP host:" };
            txtSmtpHost = new TextBox();
            var lblPort = new Label { Text = "SMTP port:" };
            nudSmtpPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 587 };
            var lblUser = new Label { Text = "SMTP username:" };
            txtSmtpUser = new TextBox();
            var lblPass = new Label { Text = "SMTP password:" };
            txtSmtpPassword = new TextBox { UseSystemPasswordChar = true };
            var lblFrom = new Label { Text = "Email from:" };
            txtSmtpFrom = new TextBox();
            var lblTo = new Label { Text = "Email to:" };
            txtSmtpTo = new TextBox();
            chkNotifySuccess = new CheckBox { Text = "Email on successful sync", Checked = true };
            chkNotifyError = new CheckBox { Text = "Email on sync errors", Checked = true };
            chkUseSsl = new CheckBox { Text = "Use SSL", Checked = true };

            emailLayout.Controls.Add(lblHost, 0, 0);
            emailLayout.Controls.Add(txtSmtpHost, 1, 0);
            emailLayout.Controls.Add(lblPort, 0, 1);
            emailLayout.Controls.Add(nudSmtpPort, 1, 1);
            emailLayout.Controls.Add(lblUser, 0, 2);
            emailLayout.Controls.Add(txtSmtpUser, 1, 2);
            emailLayout.Controls.Add(lblPass, 0, 3);
            emailLayout.Controls.Add(txtSmtpPassword, 1, 3);
            emailLayout.Controls.Add(lblFrom, 0, 4);
            emailLayout.Controls.Add(txtSmtpFrom, 1, 4);
            emailLayout.Controls.Add(lblTo, 0, 5);
            emailLayout.Controls.Add(txtSmtpTo, 1, 5);
            emailLayout.Controls.Add(chkNotifySuccess, 1, 6);
            emailLayout.Controls.Add(chkNotifyError, 1, 7);
            emailLayout.Controls.Add(chkUseSsl, 1, 8);

            tabEmail.Controls.Add(emailLayout);

            tabs.TabPages.Add(tabGeneral);
            tabs.TabPages.Add(tabSchedule);
            tabs.TabPages.Add(tabEmail);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.RightToLeft };
            var btnSave = new Button { Text = "Save", Width = 120 };
            var btnRunNow = new Button { Text = "Run sync now", Width = 120 };
            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnRunNow);

            Controls.Add(tabs);
            Controls.Add(footer);

            btnBrowseOAuth.Click += (_, __) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                        txtOAuthClientSecretPath.Text = ofd.FileName;
                }
            };

            btnAddFile.Click += (_, __) =>
            {
                using (var ofd = new OpenFileDialog { Multiselect = true })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        foreach (var path in ofd.FileNames)
                        {
                            if (!_selectedPaths.Contains(path))
                                _selectedPaths.Add(path);
                        }
                    }
                }
                RefreshPathList();
            };

            btnAddFolder.Click += (_, __) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        if (!_selectedPaths.Contains(fbd.SelectedPath))
                            _selectedPaths.Add(fbd.SelectedPath);
                    }
                }
                RefreshPathList();
            };

            btnRemovePath.Click += (_, __) =>
            {
                if (lbPaths.SelectedItem != null)
                {
                    var selected = lbPaths.SelectedItem.ToString();
                    _selectedPaths.Remove(selected);
                }
                RefreshPathList();
            };

            btnSelectGoogleFolder.Click += async (_, __) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(txtOAuthClientSecretPath.Text))
                    {
                        MessageBox.Show("Please select the OAuth client secret JSON first.");
                        return;
                    }

                    var driveService = await GoogleDriveService.CreateOAuthDriveServiceAsync(txtOAuthClientSecretPath.Text);
                    var folderService = new GoogleDriveService(driveService);
                    var folders = await folderService.GetFoldersAsync();

                    if (folders.Count == 0)
                    {
                        MessageBox.Show("No Google Drive folders were found.");
                        return;
                    }

                    using (var picker = new FolderSelectForm(folders))
                    {
                        if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedFolder != null)
                        {
                            txtDriveFolderId.Text = picker.SelectedFolder.Id;
                            txtDriveFolderName.Text = picker.SelectedFolder.Name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to select Google Drive folder:\n" + ex.Message);
                }
            };

            btnAddTime.Click += (_, __) =>
            {
                using (var picker = new TimePickerForm())
                {
                    if (picker.ShowDialog(this) == DialogResult.OK)
                    {
                        var text = picker.SelectedTime.ToString(@"hh\:mm");
                        if (!lbTimes.Items.Contains(text))
                            lbTimes.Items.Add(text);
                    }
                }
            };

            btnRemoveTime.Click += (_, __) =>
            {
                if (lbTimes.SelectedItem != null)
                    lbTimes.Items.Remove(lbTimes.SelectedItem);
            };

            btnSave.Click += (_, __) => SaveSettings();
            btnRunNow.Click += async (_, __) => await RunSyncNowAsync();
        }

        private async Task CheckForScheduledSyncAsync()
        {
            try
            {
                var runner = new SyncRunner(_settings);
                if (runner.IsDueNow(DateTime.Now))
                    await runner.RunOnceAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Scheduled sync failed: " + ex.Message);
            }
        }

        private async Task RunSyncNowAsync()
        {
            try
            {
                var runner = new SyncRunner(_settings);
                await runner.RunOnceAsync();
                MessageBox.Show("Sync completed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sync failed:\n" + ex.Message, "DriveSync");
            }
        }

        private void ShowWindow()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;

            ShowInTaskbar = true;
            Show();
            Activate();
        }

        private void CloseApplication()
        {
            _backgroundTimer.Dispose();
            _notifyIcon.Dispose();
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            _notifyIcon.Dispose();
        }

        private void RefreshPathList()
        {
            lbPaths.Items.Clear();
            foreach (var value in _selectedPaths)
                lbPaths.Items.Add(value);
        }

        private void LoadSettingsIntoForm()
        {
            txtOAuthClientSecretPath.Text = _settings.GoogleDriveOAuthClientSecretJsonPath;
            txtDriveFolderId.Text = _settings.GoogleDriveFolderId;
            txtDriveFolderName.Text = _settings.GoogleDriveFolderName;

            _selectedPaths.Clear();
            if (_settings.SelectedPaths != null)
                _selectedPaths.AddRange(_settings.SelectedPaths);

            RefreshPathList();

            chkFiles.Checked = _settings.SyncFiles;
            chkFolders.Checked = _settings.SyncFolders;
            chkSubfolders.Checked = _settings.IncludeSubFolders;

            for (int i = 0; i < clbDays.Items.Count; i++)
            {
                var day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), clbDays.Items[i].ToString());
                clbDays.SetItemChecked(i, _settings.SyncDays.Contains(day));
            }

            lbTimes.Items.Clear();
            foreach (var ts in _settings.SyncTimes)
                lbTimes.Items.Add(ts.ToString(@"hh\:mm"));

            txtSmtpHost.Text = _settings.SmtpHost;
            txtSmtpUser.Text = _settings.SmtpUsername;
            txtSmtpPassword.Text = _settings.SmtpPassword;
            txtSmtpFrom.Text = _settings.EmailFrom;
            txtSmtpTo.Text = _settings.EmailTo;
            nudSmtpPort.Value = _settings.SmtpPort;
            chkNotifySuccess.Checked = _settings.NotifyOnSuccess;
            chkNotifyError.Checked = _settings.NotifyOnError;
            chkUseSsl.Checked = _settings.SmtpUseSsl;
        }

        private void SaveSettings()
        {
            _settings.GoogleDriveOAuthClientSecretJsonPath = txtOAuthClientSecretPath.Text.Trim();
            _settings.GoogleDriveFolderId = txtDriveFolderId.Text.Trim();
            _settings.GoogleDriveFolderName = txtDriveFolderName.Text.Trim();
            _settings.SelectedPaths = new List<string>(_selectedPaths);
            _settings.SyncFiles = chkFiles.Checked;
            _settings.SyncFolders = chkFolders.Checked;
            _settings.IncludeSubFolders = chkSubfolders.Checked;

            _settings.SyncDays.Clear();
            for (int i = 0; i < clbDays.Items.Count; i++)
            {
                if (clbDays.GetItemChecked(i))
                    _settings.SyncDays.Add((DayOfWeek)Enum.Parse(typeof(DayOfWeek), clbDays.Items[i].ToString()));
            }

            _settings.SyncTimes.Clear();
            foreach (var item in lbTimes.Items)
            {
                if (TimeSpan.TryParse(item.ToString(), out var ts))
                    _settings.SyncTimes.Add(ts);
            }

            _settings.SmtpHost = txtSmtpHost.Text.Trim();
            _settings.SmtpUsername = txtSmtpUser.Text.Trim();
            _settings.SmtpPassword = txtSmtpPassword.Text;
            _settings.EmailFrom = txtSmtpFrom.Text.Trim();
            _settings.EmailTo = txtSmtpTo.Text.Trim();
            _settings.SmtpPort = (int)nudSmtpPort.Value;
            _settings.NotifyOnSuccess = chkNotifySuccess.Checked;
            _settings.NotifyOnError = chkNotifyError.Checked;
            _settings.SmtpUseSsl = chkUseSsl.Checked;

            SettingsStore.Save(_settings);
            MessageBox.Show("Settings saved.");
        }
    }

    public class TimePickerForm : Form
    {
        public TimeSpan SelectedTime { get; private set; }

        public TimePickerForm()
        {
            Text = "Select time";
            Width = 220;
            Height = 130;
            StartPosition = FormStartPosition.CenterParent;

            var dtp = new DateTimePicker { Format = DateTimePickerFormat.Time, ShowUpDown = true, Width = 120 };
            var btnOk = new Button { Text = "OK" };
            btnOk.Click += (_, __) =>
            {
                SelectedTime = dtp.Value.TimeOfDay;
                DialogResult = DialogResult.OK;
                Close();
            };

            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            panel.Controls.Add(dtp);
            panel.Controls.Add(btnOk);
            Controls.Add(panel);
        }
    }
}
