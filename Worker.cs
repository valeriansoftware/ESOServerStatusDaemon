using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ESOServerStatusDaemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITrayStatusService _trayStatusService;
        private readonly EsosStatusClient _statusClient;

        public Worker(ILogger<Worker> logger, ITrayStatusService trayStatusService, EsosStatusClient statusClient)
        {
            _logger = logger;
            _trayStatusService = trayStatusService;
            _statusClient = statusClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var summary = await _statusClient.GetStatusAsync(stoppingToken);

                    string Abbr(Megaserver s) => s switch
                    {
                        Megaserver.PcEu => "PCEU",
                        Megaserver.PcNa => "PCNA",
                        Megaserver.PcPts => "PTS",
                        Megaserver.XboxEu => "XBEU",
                        Megaserver.XboxNa => "XBNA",
                        Megaserver.PlayStationEu => "PSEU",
                        Megaserver.PlayStationNa => "PSNA",
                        _ => "?"
                    };

                    var parts = summary.Items;
                    var tooltip = "ESO Server Status Daemon";

                    _trayStatusService.UpdateSnapshot(summary);
                    _trayStatusService.UpdateStatus(new TrayStatus(summary.OverallState, tooltip));

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("ESO statuses at {time}: {statuses}", summary.CheckedAt, tooltip);
                    }
                }
                catch (Exception ex)
                {
                    _trayStatusService.UpdateStatus(new TrayStatus(TrayState.Unknown, "ESO: ошибка обновления"));
                    _logger.LogError(ex, "Ошибка обновления статуса ESO");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
