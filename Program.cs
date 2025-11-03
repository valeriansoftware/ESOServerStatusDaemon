using ESOServerStatusDaemon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IAutoStartService, AutoStartService>();
        builder.Services.AddSingleton<ITrayStatusService, TrayStatusService>();
        builder.Services.AddHttpClient<EsosStatusClient>();
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();

        host.StartAsync().GetAwaiter().GetResult();
        var trayContext = new TrayAppContext(
            host.Services.GetRequiredService<ITrayStatusService>(),
            host.Services.GetRequiredService<ISettingsService>(),
            host.Services.GetRequiredService<IAutoStartService>());
        Application.Run(trayContext);
        host.StopAsync().GetAwaiter().GetResult();
        host.Dispose();
    }
}
