using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DriveSync.Shared;
using Google.Apis.Drive.v3.Data;

namespace DriveSync.UI
{
    public class FolderSelectForm : Form
    {
        private readonly ListBox _listBox;
        private readonly List<File> _folders;
        public File SelectedFolder { get; private set; }

        public FolderSelectForm(List<File> folders)
        {
            _folders = folders;
            Text = "Select Google Drive folder";
            Width = 520;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;

            _listBox = new ListBox { Dock = DockStyle.Fill };
            foreach (var folder in folders.OrderBy(f => f.Name))
                _listBox.Items.Add(folder.Name + " (" + folder.Id + ")");

            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft };
            var btnOk = new Button { Text = "OK", Width = 90, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            footer.Controls.Add(btnOk);
            footer.Controls.Add(btnCancel);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            table.Controls.Add(_listBox, 0, 0);
            table.Controls.Add(footer, 0, 1);

            Controls.Add(table);

            btnOk.Click += (_, __) =>
            {
                if (_listBox.SelectedIndex >= 0)
                {
                    var text = _listBox.SelectedItem.ToString();
                    var start = text.LastIndexOf('(');
                    var end = text.LastIndexOf(')');
                    if (start >= 0 && end > start)
                    {
                        var id = text.Substring(start + 1, end - start - 1);
                        SelectedFolder = _folders.FirstOrDefault(f => f.Id == id);
                    }
                }
            };
        }
    }
}
