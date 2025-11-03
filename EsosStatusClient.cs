using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

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

            // 1) Пытаемся распарсить таблицу с помощью HTML парсера
            var parsed = TryParseWithHtmlAgilityPack(html, map);
            if (parsed != null)
            {
                return new ServerStatusSummary(parsed, DateTimeOffset.Now);
            }

            // 2) Фолбэк: эвристика через Regex рядом с названием
            var results = new List<SingleServerStatus>();
            foreach (var kv in map)
            {
                var probe = FindOnlineForLabel(html, kv.Value);
                if (probe == null)
                {
                    // Если не нашли — оставим Online по умолчанию, чтобы не пугать ложным Offline
                    results.Add(new SingleServerStatus(kv.Key, true));
                }
                else
                {
                    results.Add(new SingleServerStatus(kv.Key, probe.Value));
                }
            }

            return new ServerStatusSummary(results, DateTimeOffset.Now);
        }

        private static List<SingleServerStatus>? TryParseWithHtmlAgilityPack(string html, Dictionary<Megaserver, string> map)
        {
            try
            {
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                // Ищем все строки таблиц и вытаскиваем пары (название, статус)
                var rows = doc.DocumentNode.SelectNodes("//table//tr");
                if (rows == null || rows.Count == 0) return null;

                var nameToStatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tr in rows)
                {
                    var cells = tr.SelectNodes(".//td|.//th");
                    if (cells == null || cells.Count < 2) continue;
                    var name = cells[0].InnerText.Trim();
                    var status = cells[1].InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(status))
                    {
                        nameToStatus[name] = status;
                    }
                }

                if (nameToStatus.Count == 0) return null;

                var list = new List<SingleServerStatus>();
                foreach (var kv in map)
                {
                    if (nameToStatus.TryGetValue(kv.Value, out var statusText))
                    {
                        var isOnline = statusText.Contains("Online", StringComparison.OrdinalIgnoreCase);
                        var isOffline = statusText.Contains("Offline", StringComparison.OrdinalIgnoreCase);
                        if (isOnline || isOffline)
                        {
                            list.Add(new SingleServerStatus(kv.Key, isOnline));
                        }
                    }
                }

                return list.Count > 0 ? list : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool? FindOnlineForLabel(string html, string label)
        {
            // Ищем ближайшее упоминание Online/Offline рядом с названием сервера
            var pattern = Regex.Escape(label) + @"[\s\S]{0,200}?(Online|Offline)";
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                // Не нашли уверенно
                return null;
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


