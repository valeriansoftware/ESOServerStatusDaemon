using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace ESOServerStatusDaemon
{
    public sealed class StatusForm : Form
    {
        private readonly ITrayStatusService _trayStatusService;
        private readonly ListView _listView;

        private static readonly Dictionary<Megaserver, string> DisplayNames = new()
        {
            { Megaserver.PcEu, "PC EU Megaserver" },
            { Megaserver.PcNa, "PC NA Megaserver" },
            { Megaserver.PcPts, "PC PTS Megaserver" },
            { Megaserver.XboxEu, "XBOX EU Megaserver" },
            { Megaserver.XboxNa, "XBOX NA Megaserver" },
            { Megaserver.PlayStationEu, "PlayStation EU Megaserver" },
            { Megaserver.PlayStationNa, "PlayStation NA Megaserver" },
        };

        public StatusForm(ITrayStatusService trayStatusService)
        {
            _trayStatusService = trayStatusService;
            Text = "ESO Server Status";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 240);

            // Иконка формы — та же, что у exe
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!; } catch { }

            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _listView.Columns.Add("Сервер", 340, HorizontalAlignment.Left);
            _listView.Columns.Add("Статус", 140, HorizontalAlignment.Left);
            Controls.Add(_listView);

            Shown += (_, _) => RefreshFromLatest();
            _trayStatusService.SummaryChanged += OnSummaryChanged;
            FormClosed += (_, _) => _trayStatusService.SummaryChanged -= OnSummaryChanged;
        }

        private void OnSummaryChanged(object? sender, ServerStatusSummary e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshFromLatest));
            }
            else
            {
                RefreshFromLatest();
            }
        }

        private void RefreshFromLatest()
        {
            var summary = _trayStatusService.LatestSummary;
            _listView.BeginUpdate();
            try
            {
                _listView.Items.Clear();
                if (summary == null)
                {
                    foreach (var server in DisplayNames.Keys)
                    {
                        var li = new ListViewItem(DisplayNames[server]);
                        li.SubItems.Add("—");
                        _listView.Items.Add(li);
                    }
                    return;
                }

                foreach (var server in DisplayNames.Keys)
                {
                    var item = summary.Items.FirstOrDefault(i => i.Server == server);
                    var li = new ListViewItem(DisplayNames[server]);
                    var statusText = item == null ? "—" : (item.IsOnline ? "Online" : "Offline");
                    li.SubItems.Add(statusText);
                    li.ForeColor = item == null ? SystemColors.ControlText : (item.IsOnline ? Color.Green : Color.DarkRed);
                    _listView.Items.Add(li);
                }
            }
            finally
            {
                _listView.EndUpdate();
            }
        }
    }
}


