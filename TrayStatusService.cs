using System;

namespace ESOServerStatusDaemon
{
    public enum TrayState
    {
        Unknown,
        Online,
        Degraded,
        Offline
    }

    public sealed class TrayStatus
    {
        public TrayState State { get; }
        public string Tooltip { get; }

        public TrayStatus(TrayState state, string tooltip)
        {
            State = state;
            Tooltip = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
        }
    }

    public interface ITrayStatusService
    {
        event EventHandler<TrayStatus>? StatusChanged;
        event EventHandler<ServerStatusSummary>? SummaryChanged;
        event EventHandler<string>? NotificationRequested;
        ServerStatusSummary? LatestSummary { get; }
        void UpdateStatus(TrayStatus status);
        void UpdateSnapshot(ServerStatusSummary summary);
    }

    public sealed class TrayStatusService : ITrayStatusService
    {
        private readonly ISettingsService _settings;
        public event EventHandler<TrayStatus>? StatusChanged;
        public event EventHandler<ServerStatusSummary>? SummaryChanged;
        public event EventHandler<string>? NotificationRequested;
        public ServerStatusSummary? LatestSummary { get; private set; }

        public TrayStatusService(ISettingsService settings)
        {
            _settings = settings;
        }

        public void UpdateStatus(TrayStatus status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public void UpdateSnapshot(ServerStatusSummary summary)
        {
            var previous = LatestSummary;
            LatestSummary = summary;
            SummaryChanged?.Invoke(this, summary);

            if (!_settings.NotificationsEnabled || previous == null)
            {
                return;
            }

            // Определяем изменения статусов
            var changes = new List<string>();
            foreach (var curr in summary.Items)
            {
                var prev = previous.Items.FirstOrDefault(i => i.Server == curr.Server);
                if (prev != null && prev.IsOnline != curr.IsOnline)
                {
                    var name = EsosStatusClient.GetDisplayName(curr.Server);
                    changes.Add($"{name}: {(curr.IsOnline ? "Online" : "Offline")}");
                }
            }

            if (changes.Count > 0)
            {
                var message = string.Join("\n", changes);
                NotificationRequested?.Invoke(this, message);
            }
        }
    }
}


