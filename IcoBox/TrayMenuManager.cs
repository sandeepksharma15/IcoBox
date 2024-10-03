using Microsoft.Extensions.DependencyInjection;

namespace IcoBox;

public class TrayMenuManager
{
    private readonly NotifyIcon trayIcon;
    private readonly ContextMenuStrip trayMenu;
    private readonly IServiceProvider serviceProvider;

    public TrayMenuManager(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        trayMenu = new ContextMenuStrip();
        trayIcon = new NotifyIcon
        {
            Text = "Icon Box",
            Icon = new Icon(Path.Combine(MainApp.IconsFolder!, "IcoBox.ico"), 40, 40),
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        InitializeMenu();
    }

    private void InitializeMenu()
    {
        trayMenu.Items.Add("About", null, AboutIcoBox);
        trayMenu.Items.Add("New Icon Box", null, CreateIconGroup);
        trayMenu.Items.Add("Start with Windows", null, ToggleLoadAtStartup);
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("Exit", null, OnExit);

        // Set the checked state for startup menu
        UpdateStartupMenuCheckState();
    }

    private void UpdateStartupMenuCheckState()
    {
        var loadAtStartupMenuItem = (ToolStripMenuItem)trayMenu.Items[2];
        loadAtStartupMenuItem.Checked = Helpers.IsInStartup(AppInfo.AppName);
    }

    private void ToggleLoadAtStartup(object? sender, EventArgs e)
    {
        if (Helpers.IsInStartup(AppInfo.AppName))
            Helpers.RemoveFromStartup(AppInfo.AppName);
        else
            Helpers.AddToStartup(AppInfo.AppName, Application.ExecutablePath);

        // Update checked state after toggling
        UpdateStartupMenuCheckState();
    }

    private void CreateIconGroup(object? sender, EventArgs e) => new IconBox().Show();

    private void AboutIcoBox(object? sender, EventArgs e) => MessageBox.Show("Show About Box");

    private void OnExit(object? sender, EventArgs e)
    {
        serviceProvider
            .GetRequiredService<IAppStateService>()
            .SaveAppState();

        Application.Exit();
    }

    public void Dispose() => trayIcon.Dispose();
}
