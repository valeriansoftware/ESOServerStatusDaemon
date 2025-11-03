using System;
using System.Drawing;
using System.Windows.Forms;

namespace ESOServerStatusDaemon
{
    public sealed class TrayAppContext : ApplicationContext
    {
        private readonly ITrayStatusService _trayStatusService;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _menu;
        private StatusForm? _statusForm;
        private readonly ISettingsService _settings;
        private readonly IAutoStartService _autoStart;
        private readonly Icon _appIcon;

        public TrayAppContext(ITrayStatusService trayStatusService, ISettingsService settings, IAutoStartService autoStart)
        {
            _trayStatusService = trayStatusService;
            _settings = settings;
            _autoStart = autoStart;

            _menu = new ContextMenuStrip();
            var notificationsItem = new ToolStripMenuItem("Включить уведомления") { CheckOnClick = true, Checked = _settings.NotificationsEnabled };
            notificationsItem.Click += (_, _) => { _settings.NotificationsEnabled = notificationsItem.Checked; };
            _menu.Items.Add(notificationsItem);

            //var summaryItem = new ToolStripMenuItem("Краткая сводка");
            //summaryItem.Click += (_, _) => ShowSummaryBalloon();
            //_menu.Items.Add(summaryItem);

            var autostartItem = new ToolStripMenuItem("Автозагрузка") { CheckOnClick = true, Checked = _settings.AutoStartEnabled || _autoStart.IsEnabled() };
            autostartItem.Click += (_, _) =>
            {
                if (autostartItem.Checked)
                {
                    _autoStart.Enable();
                    _settings.AutoStartEnabled = true;
                }
                else
                {
                    _autoStart.Disable();
                    _settings.AutoStartEnabled = false;
                }
            };
            _menu.Items.Add(autostartItem);

            var exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += (_, _) => ExitThread();
            _menu.Items.Add(exitItem);

            _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!;

            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Text = "ESO Server Status: инициализация...",
                ContextMenuStrip = _menu,
                Icon = _appIcon
            };

            _trayStatusService.StatusChanged += OnStatusChanged;
            _notifyIcon.MouseClick += OnNotifyIconClick;
            _trayStatusService.NotificationRequested += OnNotificationRequested;
        }

        protected override void ExitThreadCore()
        {
            _notifyIcon.Visible = false;
            _trayStatusService.StatusChanged -= OnStatusChanged;
            _notifyIcon.MouseClick -= OnNotifyIconClick;
            _trayStatusService.NotificationRequested -= OnNotificationRequested;
            _notifyIcon.Dispose();
            base.ExitThreadCore();
        }

        private void OnStatusChanged(object? sender, TrayStatus e)
        {
            _notifyIcon.Text = e.Tooltip;
            _notifyIcon.Icon = _appIcon;
        }

        private void OnNotifyIconClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_statusForm == null || _statusForm.IsDisposed)
            {
                _statusForm = new StatusForm(_trayStatusService);
            }
            if (_statusForm.Visible)
            {
                _statusForm.Activate();
            }
            else
            {
                _statusForm.Show();
            }
        }

        private void OnNotificationRequested(object? sender, string message)
        {
            if (!_settings.NotificationsEnabled) return;
            _notifyIcon.BalloonTipTitle = "ESO: изменения статусов";
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(5000);
        }

        private void ShowSummaryBalloon()
        {
            var summary = _trayStatusService.LatestSummary;
            if (summary == null) return;
            var lines = summary.Items
                .Select(i => EsosStatusClient.GetDisplayName(i.Server) + ": " + (i.IsOnline ? "Online" : "Offline"));
            var text = string.Join("\n", lines);
            _notifyIcon.BalloonTipTitle = "ESO Server Status Daemon";
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
            _notifyIcon.ShowBalloonTip(7000);
        }
    }
}


