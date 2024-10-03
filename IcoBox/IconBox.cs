using System;
using System.Diagnostics;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

using IWshRuntimeLibrary;

using static IcoBox.MainApp; // Add this for handling .lnk files (COM interop)

namespace IcoBox;

public class IconBox : Form
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetParent(nint hWndChild, nint hWndNewParent);

    [DllImport("user32.dll")]
    private static extern nint GetDesktopWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly nint HWND_BOTTOM = new(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOZORDER = 0x0004;

    private const int HEADER_HEIGHT = 30;

    private bool isDragging;
    private Point dragStartPoint;

    private readonly Label titleLabel;
    private readonly Panel headerPanel;
    private readonly ListView iconListView;
    private ContextMenuStrip contextMenuStrip;
    private readonly PictureBox iconPictureBox;

    private readonly List<(string FilePath, string FileName)> movedFiles = []; // To store the file paths and names of moved files

    private static Helpers.IconMetrics IconMetrics = new();

    public static string? AppFolder
    {
        get
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolder, AppInfo.AppName);

            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            return appFolder;
        }
    }

    public IconBox(WindowData window) :
        this(new Rectangle(window.X, window.Y, window.Width, window.Height), window.Title, window.IconPaths)
    {
    }

    public IconBox(Rectangle? bounds = null, string? title = null, List<string>? iconPaths = null)
    {
        SetWindowAppearance(bounds);

        // Allow Drag And Drop
        AllowDrop = true;

        // Create The Icon PictureBox
        iconPictureBox = GetIconPictureBox();

        // Create The Title Label
        titleLabel = CreateTitleLabel(title);

        // Create Title Bar
        headerPanel = CreateHeaderPanel();

        // Create The Icon List View
        iconListView = CreateIconListView(Height, Width, iconPaths);

        // Subscribe To Drag Events
        iconListView.DragEnter += new DragEventHandler(OnDragEnter);
        iconListView.DragDrop += new DragEventHandler(OnDragDrop);
        iconListView.ItemDrag += IconListView_ItemDrag;

        // Subscribe to the Click event to handle icon clicks
        iconListView.ItemActivate += new EventHandler(OnItemActivate);

        titleLabel.DoubleClick += TitleLabel_DoubleClick; // Double click to edit title

        headerPanel.Controls.Add(iconPictureBox);
        headerPanel.Controls.Add(titleLabel);

        headerPanel.MouseMove += HeaderPanel_MouseMove; // Mouse move to enable dragging
        headerPanel.MouseDown += HeaderPanel_MouseDown; // Mouse down to start dragging
        headerPanel.MouseUp += HeaderPanel_MouseUp; // Mouse up to stop dragging

        Controls.Add(headerPanel);
        Controls.Add(iconListView);

        // Apply Theme Colors
        ApplyThemeColors();

        // Setup Context Menu
        contextMenuStrip = GetContextMenu();
        MouseDown += ShowContextMenu;
        foreach (Control control in this.Controls)
            control.MouseDown += ShowContextMenu;

        // Custom Resize Handler
        Resize += (s, e) => 
        { 
            iconListView.Top = HEADER_HEIGHT;

            int availableHeight = ClientSize.Height - HEADER_HEIGHT;
            iconListView.Height = availableHeight;
        };
    }

    private void ApplyThemeColors()
    {
        // BackColor = SystemColors.ActiveCaption;

        //headerPanel.BackColor = SystemColors.ActiveCaption;
        titleLabel.ForeColor = SystemColors.ActiveCaptionText;

        iconListView.BackColor = Color.LightGray;
        iconListView.ForeColor = SystemColors.WindowText;
    }

    private void ShowContextMenu(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
            contextMenuStrip.Show(this, e.Location);
    }

    private ContextMenuStrip GetContextMenu()
    {
        contextMenuStrip = new ContextMenuStrip();
        contextMenuStrip.Items.Add("Auto Arrange Items", null, AutoArrangeItems);
        contextMenuStrip.Items.Add("Arrange Items By Name", null, ArrangeItemsByName);
        contextMenuStrip.Items.Add("Arrange Items By Type", null, ArrangeItemsByType);
        contextMenuStrip.Items.Add("-");
        contextMenuStrip.Items.Add("Remove This Icon Box", null, RemoveThisIconBox);

        // Set the AutoArrange menu item checked state
        var autoArrangeMenuItem = (ToolStripMenuItem)contextMenuStrip.Items[0];
        autoArrangeMenuItem.Checked = iconListView.AutoArrange;

        return contextMenuStrip;
    }

    private void AutoArrangeItems(object? sender, EventArgs e)
    {
        // Toggle the AutoArrange property of the ListView
        iconListView.AutoArrange = !iconListView.AutoArrange;

        // Set the AutoArrange menu item checked state
        var autoArrangeMenuItem = (ToolStripMenuItem)contextMenuStrip.Items[0];
        autoArrangeMenuItem.Checked = iconListView.AutoArrange;
    }

    private void RemoveThisIconBox(object? sender, EventArgs e)
    {
        if (iconListView.Items.Count > 0)
            MessageBox.Show("Please remove all icons before remnoving the Icon Box...", "Remove Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        else
            Close();
    }

    private void ArrangeItemsByType(object? sender, EventArgs e)
    {
        // Get The Menu Item
        var menuItem = (ToolStripMenuItem)contextMenuStrip.Items[2];

        if (menuItem.Checked is false)
        {
            iconListView.ListViewItemSorter = new ListViewItemComparer(ArrangeType.ByType, true);
            iconListView.Sort();

            // Update Menu Item Checked State
            menuItem.Checked = true;

            // Uncheck the other menu item
            var otherMenuItem = (ToolStripMenuItem)contextMenuStrip.Items[1];
            otherMenuItem.Checked = false;
        }
        else
        {
            menuItem.Checked = false;
            iconListView.Sorting = SortOrder.None;
        }
    }

    private void ArrangeItemsByName(object? sender, EventArgs e)
    {
        // Get The Menu Item
        var menuItem = (ToolStripMenuItem)contextMenuStrip.Items[1];

        if (menuItem.Checked is false)
        {
            iconListView.ListViewItemSorter = new ListViewItemComparer(ArrangeType.ByName, true);
            iconListView.Sort();

            // Update Menu Item Checked State
            menuItem.Checked = true;

            // Uncheck the other menu item
            var otherMenuItem = (ToolStripMenuItem)contextMenuStrip.Items[2];
            otherMenuItem.Checked = false;
        }
        else
        {
            menuItem.Checked = false;
            iconListView.Sorting = SortOrder.None;
        }
    }

    // Item drag event handler
    private void IconListView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        List<string> filePaths = [];
        List<ListViewItem> itemsToRemove = [];

        foreach (ListViewItem selectedItem in iconListView.SelectedItems)
        {
            string filePath = selectedItem.Tag!.ToString()!;
            filePaths.Add(filePath);
            itemsToRemove.Add(selectedItem); // Collect items to remove after drag
        }

        if (filePaths.Count > 0)
        {
            DataObject data = new(DataFormats.FileDrop, filePaths.ToArray());
            DragDropEffects result = iconListView.DoDragDrop(data, DragDropEffects.Move);

            // If the operation was successful, remove the dragged items from the ListView
            if (result == DragDropEffects.Move)
                foreach (ListViewItem item in itemsToRemove)
                    iconListView.Items.Remove(item); // Remove from ListView
        }
    }

    // Handle the DragEnter event
    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        // Check if the data being dragged is a file (desktop icon)
        if (e.Data!.GetDataPresent(DataFormats.FileDrop))
            // Show copy effect to indicate the user can drop the file
            e.Effect = DragDropEffects.Move;
        else
            // If not a valid file drop, show no effect
            e.Effect = DragDropEffects.None;
    }

    // Handle the DragDrop event
    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        // Check if the dropped data is in FileDrop format
        if (e.Data!.GetDataPresent(DataFormats.FileDrop))
        {
            // Get the file paths that were dropped
            string[] files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;

            // Process the dropped files
            foreach (string filePath in files)
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string destinationPath = Path.Combine(AppFolder!, fileName);

                    // Move the file to the destination folder
                    System.IO.File.Move(filePath, destinationPath);

                    // Get the file name without the .lnk extension
                    string displayName = Path.GetFileNameWithoutExtension(fileName);

                    // Create a ListViewItem for the moved file and store the file path in the Tag
                    ListViewItem item = new(displayName)
                    {
                        ImageKey = fileName, // Use the filename as the key
                        Tag = destinationPath // Store the file path in the Tag property
                    };

                    // Extract and set the icon for the item
                    Icon fileIcon = Icon.ExtractAssociatedIcon(destinationPath)
                        ?? new Icon(SystemIcons.Application, 48, 48);

                    iconListView.LargeImageList!.Images.Add(fileName, fileIcon);
                    iconListView.Items.Add(item);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }
    }

    // Handle the click event to open the file
    private void OnItemActivate(object? sender, EventArgs e)
    {
        if (iconListView.SelectedItems.Count > 0)
        {
            ListViewItem selectedItem = iconListView.SelectedItems[0];
            string? filePath = selectedItem.Tag!.ToString(); // Retrieve the file path from the Tag

            try
            {
                // If the file is a .lnk shortcut, resolve its target
                if (Path.GetExtension(filePath!).Equals(".lnk", StringComparison.CurrentCultureIgnoreCase))
                    filePath = ResolveShortcut(filePath!);

                // Create a process to open the file, requesting elevation if necessary
                ProcessStartInfo psi = new(filePath!)
                {
                    UseShellExecute = true,
                    Verb = "runas" // This requests elevation
                };

                // Open the file, folder, or application
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) // The operation was canceled by the user (User declined the UAC prompt)
                {
                    MessageBox.Show("You declined the elevation request.", "Operation Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Unable to open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Method to resolve the target of a .lnk shortcut
    private string ResolveShortcut(string shortcutPath)
    {
        // Create a new WshShell object to work with the shortcut
        WshShell shell = new();
        IWshShortcut link = (IWshShortcut)shell.CreateShortcut(shortcutPath);

        // Return the full path of the target file or application
        return link.TargetPath;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ShowInTaskbar = false; // Don't show in taskbar

        // Get the desktop window handle
        nint desktopHandle = GetDesktopWindow();

        // Set the desktop as the parent of this window
        SetParent(Handle, desktopHandle);

        // Set window position to the bottom of the Z-order (below all other windows)
        SetWindowPos(Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_NOZORDER);
    }

    private void TitleLabel_DoubleClick(object? sender, EventArgs e)
    {
        // Allow editing of the title label on double click
        TextBox textBox = new()
        {
            Text = titleLabel.Text,
            Bounds = titleLabel.Bounds, // Position the textbox
            BackColor = SystemColors.Window, // Match the system background color
            BorderStyle = BorderStyle.FixedSingle, // No border
            Font = Font = new Font("Tahoma", 13)
        };

        Controls.Add(textBox);
        textBox.BringToFront(); // Ensure textbox is drawn in front of the title label
        textBox.Focus(); // Focus on the textbox for immediate editing
        textBox.SelectAll(); // Select all text

        // Update title when text changes
        textBox.Leave += (s, args) => ConfirmTitleChange(textBox);

        // Handle pressing Enter or Tab
        textBox.KeyDown += (s, keyArgs) =>
        {
            if (keyArgs.KeyCode == Keys.Enter || keyArgs.KeyCode == Keys.Tab)
            {
                // Simulate leaving the TextBox when Enter or Tab is pressed
                ConfirmTitleChange(textBox);
                keyArgs.Handled = true;
            }
        };
    }

    private void ConfirmTitleChange(TextBox textBox)
    {
        titleLabel.Text = textBox.Text;
        Controls.Remove(textBox); // Remove the textbox after editing is complete
    }

    private void HeaderPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            // Move the window by adjusting its location
            Left += e.X - dragStartPoint.X;
            Top += e.Y - dragStartPoint.Y;
        }
    }

    private void HeaderPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isDragging = true;
            dragStartPoint = e.Location;
        }
    }

    private void HeaderPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        isDragging = false;
    }

    private static Rectangle GetWindowBounds()
    {
        // Calculate and set window size based on icon size and spacing
        IconMetrics = Helpers.GetDesktopIconMetrics();

        // Calculate window size for 3 rows and 5 columns of icons
        int windowWidth = 5 * IconMetrics.SpacingHorizontal;
        int windowHeight = 3 * IconMetrics.SpacingVertical + HEADER_HEIGHT; // Add height for title bar

        // Get screen dimensions
        var screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        var screenHeight = Screen.PrimaryScreen!.Bounds.Height;

        // Calculate position (right side aligned, bottom 100px above the screen)
        int x = screenWidth - windowWidth - 10;  // Position x such that right side touches viewport (with 10px padding)
        int y = screenHeight - windowHeight - 100;  // Position y 100px above bottom of screen

        return new Rectangle(x, y, windowWidth, windowHeight);
    }

    private static Panel CreateHeaderPanel()
    {
        return new Panel
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = HEADER_HEIGHT,
            Cursor = Cursors.SizeAll // Change cursor to hand over title bar
        };
    }

    private static Label CreateTitleLabel(string? title)
    {
        title ??= AppInfo.NewBoxTitle;

        return new Label
        {
            Text = title,
            AutoSize = true,
            Height = HEADER_HEIGHT,
            ForeColor = Color.Black,
            Cursor = Cursors.Default,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Tahoma", 16),
            Left = 25,
        };
    }

    private static PictureBox GetIconPictureBox()
    {
        var iconPictureBox = new PictureBox
        {
            Size = new Size(16, 16),
            Location = new Point(7, 5),
            SizeMode = PictureBoxSizeMode.StretchImage
        };

        iconPictureBox.Image = new Icon(Path.Combine(IconsFolder!, "IcoBox III (16x16).ico"), 16, 16).ToBitmap();

        return iconPictureBox;
    }

    private static ListView CreateIconListView(int height, int width, List<string>? iconPaths)
    {
        ImageList imageList = new();
        imageList.ImageSize = new Size(40, 40); // Size of icons

        var iconListView = new ListView
        {
            View = View.LargeIcon,
            LargeImageList = imageList,
            AllowDrop = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Top = HEADER_HEIGHT,
            Width = width,
            Height = height - HEADER_HEIGHT,
            Left = 0
        };

        foreach (string filePath in iconPaths ?? [])
        {
            string fileName = Path.GetFileName(filePath);
            string destinationPath = Path.Combine(AppFolder!, fileName);

            // Get the file name without the .lnk extension
            string displayName = Path.GetFileNameWithoutExtension(fileName);

            // Create a ListViewItem for the moved file and store the file path in the Tag
            ListViewItem item = new(displayName)
            {
                ImageKey = fileName, // Use the filename as the key
                Tag = destinationPath // Store the file path in the Tag property
            };

            // Extract and set the icon for the item
            Icon fileIcon = Icon.ExtractAssociatedIcon(destinationPath)
                ?? new Icon(SystemIcons.Application, 48, 48);

            iconListView.LargeImageList!.Images.Add(fileName, fileIcon);
            iconListView.Items.Add(item);
        }

        return iconListView;
    }

    private void SetWindowAppearance(Rectangle? bounds = null)
    {
        bounds ??= GetWindowBounds(); // Get bounds for the window

        // Set window properties
        FormBorderStyle = FormBorderStyle.Sizable;
        ControlBox = false; // No control box
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds.Value;
        ShowInTaskbar = false; // Don't show in taskbar
        Opacity = 1.0;

        Padding = new Padding(0);
    }
}