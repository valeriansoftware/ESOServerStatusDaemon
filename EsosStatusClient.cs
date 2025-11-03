using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ESOServerStatusDaemon
{
    public enum Megaserver
    {
        PcEu,
        PcNa,
        PcPts,
        XboxEu,
        XboxNa,
        PlayStationEu,
        PlayStationNa
    }

    public sealed class SingleServerStatus
    {
        public Megaserver Server { get; }
        public bool IsOnline { get; }

        public SingleServerStatus(Megaserver server, bool isOnline)
        {
            Server = server;
            IsOnline = isOnline;
        }
    }

    public sealed class ServerStatusSummary
    {
        public IReadOnlyList<SingleServerStatus> Items { get; }
        public DateTimeOffset CheckedAt { get; }

        public TrayState OverallState
        {
            get
            {
                var anyOffline = false;
                foreach (var s in Items)
                {
                    if (!s.IsOnline)
                    {
                        anyOffline = true;
                        break;
                    }
                }
                return anyOffline ? TrayState.Offline : TrayState.Online;
            }
        }

        public ServerStatusSummary(IReadOnlyList<SingleServerStatus> items, DateTimeOffset checkedAt)
        {
            Items = items;
            CheckedAt = checkedAt;
        }
    }

    public sealed class EsosStatusClient
    {
        private readonly HttpClient _httpClient;

        public EsosStatusClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ESOServerStatusDaemon/1.0");
        }

        public async Task<ServerStatusSummary> GetStatusAsync(CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync("https://esoserverstatus.net/", cancellationToken);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            var map = new Dictionary<Megaserver, string>
            {
                { Megaserver.PcEu, "PC EU Megaserver" },
                { Megaserver.PcNa, "PC NA Megaserver" },
                { Megaserver.PcPts, "PC PTS Megaserver" },
                { Megaserver.XboxEu, "XBOX EU Megaserver" },
                { Megaserver.XboxNa, "XBOX NA Megaserver" },
                { Megaserver.PlayStationEu, "PlayStation EU Megaserver" },
                { Megaserver.PlayStationNa, "PlayStation NA Megaserver" },
            };

            var results = new List<SingleServerStatus>();
            foreach (var kv in map)
            {
                var isOnline = FindOnlineForLabel(html, kv.Value);
                results.Add(new SingleServerStatus(kv.Key, isOnline));
            }

            return new ServerStatusSummary(results, DateTimeOffset.Now);
        }

        private static bool FindOnlineForLabel(string html, string label)
        {
            // Ищем ближайшее упоминание Online/Offline рядом с названием сервера
            var pattern = Regex.Escape(label) + @"[\s\S]{0,200}?(Online|Offline)";
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                // fallback: ищем таблицу по названию, иначе считаем Unknown как Offline чтобы не пропустить проблемы
                return false;
            }
            var val = match.Groups[1].Value;
            return val.Equals("Online", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetDisplayName(Megaserver server) => server switch
        {
            Megaserver.PcEu => "PC EU Megaserver",
            Megaserver.PcNa => "PC NA Megaserver",
            Megaserver.PcPts => "PC PTS Megaserver",
            Megaserver.XboxEu => "XBOX EU Megaserver",
            Megaserver.XboxNa => "XBOX NA Megaserver",
            Megaserver.PlayStationEu => "PlayStation EU Megaserver",
            Megaserver.PlayStationNa => "PlayStation NA Megaserver",
            _ => server.ToString()
        };
    }
}


