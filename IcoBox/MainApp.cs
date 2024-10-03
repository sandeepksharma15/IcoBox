using Microsoft.Extensions.DependencyInjection;

namespace IcoBox;

public class MainApp : Form
{
    private readonly IServiceProvider serviceProvider;
    private TrayMenuManager? trayMenuManager;

    public static string? IconsFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        Application.Run(new MainApp());
    }

    public MainApp()
    {
        // Register Services
        serviceProvider = AppServices.RegisterServices();

        // Restore window state
        serviceProvider
            .GetRequiredService<IAppStateService>()
            .RestoreAppState();

        // Initialize tray menu manager
        trayMenuManager = new TrayMenuManager(serviceProvider);
    }

    protected override void OnLoad(EventArgs e)
    {
        Visible = false; // Hide form window
        ShowInTaskbar = false; // Remove from taskbar
        base.OnLoad(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            trayMenuManager?.Dispose();

        base.Dispose(disposing);
    }
}
