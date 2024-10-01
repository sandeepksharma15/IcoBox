using System.Diagnostics;
using System.Text.Json;

namespace IcoBox;

public class MainApp : Form
{
    private NotifyIcon? trayIcon;
    private ContextMenuStrip? trayMenu;

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
        // Restore window state
        RestoreWindowState();

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
        SaveWindowState();
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

    [Serializable]
    public class WindowData
    {
        public string? Title { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public List<string>? IconPaths { get; set; } // Paths to icons/folders
    }

    private static void RestoreWindowState()
    {
        Debug.WriteLine("Restoring window state");

        string filePath = GetSaveFile;

        if (!File.Exists(filePath)) return;

        // Deserialize window state
        string json = File.ReadAllText(filePath);
        List<WindowData>? windowsData = JsonSerializer.Deserialize<List<WindowData>>(json);

        // Nothing To Restore
        if (windowsData == null) return;

        // Recreate windows
        foreach (var windowData in windowsData)
        {
            var window = new IconBox(windowData);
            window.Show();
        }
    }

    private static void SaveWindowState()
    {
        Debug.WriteLine("Saving window state");

        List<WindowData> windowsData = [];

        foreach (IconBox window in Application.OpenForms.OfType<IconBox>())
        {
            var headerPanel = window.Controls.OfType<Panel>().FirstOrDefault();
            var titleLabel = headerPanel?.Controls.OfType<Label>().FirstOrDefault();

            var windowData = new WindowData
            {
                Title = titleLabel?.Text,
                Width = window.Width,
                Height = window.Height,
                X = window.Left,
                Y = window.Top,
                IconPaths = []
            };

            // Assuming your ListView stores icon paths in Tag property of each item
            foreach (ListViewItem item in (window.Controls.OfType<ListView>().FirstOrDefault()!).Items)
                windowData.IconPaths.Add(item.Tag!.ToString()!); // Store icon file path

            windowsData.Add(windowData);
        }

        // Serialize and save window state
        File.WriteAllText(GetSaveFile, JsonSerializer.Serialize(windowsData));
    }

    private static string GetSaveFile
        => Path.Combine(IconBox.AppFolder!, AppInfo.SaveFileName);
}
