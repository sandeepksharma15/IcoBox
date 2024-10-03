using Microsoft.Extensions.DependencyInjection;

namespace IcoBox;

public class MainApp : Form
{
    private NotifyIcon? trayIcon;
    private ContextMenuStrip? trayMenu;
    private IServiceProvider serviceProvider;

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

        DisplayTrayMenu();
    }

    private void DisplayTrayMenu()
    {
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("About", null, AboutIcoBox);
        trayMenu.Items.Add("New Icon Box", null, CreateIconGrpup);
        trayMenu.Items.Add("Start with Windows", null, LoadAtStartup);
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("Exit", null, OnExit!);

        // Set the checked state
        UpdateStartupMenuCheckState();

        // Create a tray icon
        trayIcon = new NotifyIcon();
        trayIcon.Text = "Icon Box";
        trayIcon.Icon = new Icon(Path.Combine(IconsFolder!, "IcoBox.ico"), 40, 40);

        // Add menu to tray icon
        trayIcon.ContextMenuStrip = trayMenu;

        // Show the tray icon
        trayIcon.Visible = true;
    }

    private void UpdateStartupMenuCheckState()
    {
        var loadAtStartupMenuItem = (ToolStripMenuItem)trayMenu!.Items[2];
        loadAtStartupMenuItem.Checked = Helpers.IsInStartup(AppInfo.AppName);
    }

    private void LoadAtStartup(object? sender, EventArgs e)
    {
        if (Helpers.IsInStartup(AppInfo.AppName))
            Helpers.RemoveFromStartup(AppInfo.AppName);
        else
            Helpers.AddToStartup(AppInfo.AppName, Application.ExecutablePath);

        // Set the checked state
        UpdateStartupMenuCheckState();
    }

    private void CreateIconGrpup(object? sender, EventArgs e)
    {
        new IconBox().Show();
    }

    private void AboutIcoBox(object? sender, EventArgs e)
    {
        MessageBox.Show("Show About Box");
    }

    // Exit action
    private void OnExit(object sender, EventArgs e)
    {
        serviceProvider
            .GetRequiredService<IAppStateService>()
            .SaveAppState();

        Application.Exit();
    }

    protected override void OnLoad(EventArgs e)
    {
        Visible = false; // Hide form window
        ShowInTaskbar = false; // Remove from taskbar
        base.OnLoad(e);
    }

    protected override void Dispose(bool disposing)
    {
        // Clean up tray icon
        if (disposing && trayIcon != null)
            trayIcon.Dispose();

        base.Dispose(disposing);
    }
}
