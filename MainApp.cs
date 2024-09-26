using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IcoBox;
public class MainApp : Form
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;

    [STAThread]
    public static void Main()
    {
        Application.Run(new MainApp());
    }

    public MainApp()
    {
        // Create a simple tray menu with a few items
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("New Icon Group", null, CreateIconGrpup!);
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("Exit", null, OnExit!);

        // Create a tray icon
        trayIcon = new NotifyIcon();
        trayIcon.Text = "Icon Group";
        trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

        // Add menu to tray icon
        trayIcon.ContextMenuStrip = trayMenu;

        // Show the tray icon
        trayIcon.Visible = true;
    }

    private void CreateIconGrpup(object sender, EventArgs e)
    {
        MessageBox.Show("Option 1 clicked!");
    }

    // Exit action
    private void OnExit(object sender, EventArgs e)
    {
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
        {
            trayIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
