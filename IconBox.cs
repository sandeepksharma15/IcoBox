using System.Reflection;
using System.Runtime.InteropServices;

namespace iFence;


public class IconBox : Form
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ICONMETRICS
    {
        public int cbSize;
        public int iHorzSpacing;
        public int iVertSpacing;
        public int iTitleWrap;
        public int lfFont;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref ICONMETRICS pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out int pvParam, uint fWinIni);

    const uint SPI_GETICONMETRICS = 0x002D;
    const uint SPI_GETICONSPACING = 0x002F;

    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOZORDER = 0x0004;

    private bool isDragging;
    private readonly string title = "Icon Box";
    private Point dragStartPoint;
    private readonly Label titleLabel;
    private readonly Panel headerPanel;
    private const int HEADER_HEIGHT = 30;

    public IconBox()
    {
        SetWindowAppearance();

        // Create The Title Label
        titleLabel = CreateTitleLabel();

        // Create Title Bar
        headerPanel = CreateHeaderPanel();

        titleLabel.DoubleClick += TitleLabel_DoubleClick; // Double click to edit title

        headerPanel.Controls.Add(titleLabel);

        headerPanel.MouseMove += HeaderPanel_MouseMove; // Mouse move to enable dragging
        headerPanel.MouseDown += HeaderPanel_MouseDown; // Mouse down to start dragging
        headerPanel.MouseUp += HeaderPanel_MouseUp; // Mouse up to stop dragging

        Controls.Add(headerPanel);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ShowInTaskbar = false; // Don't show in taskbar

        // Get the desktop window handle
        IntPtr desktopHandle = GetDesktopWindow();

        // Set the desktop as the parent of this window
        SetParent(this.Handle, desktopHandle);

        // Set window position to the bottom of the Z-order (below all other windows)
        SetWindowPos(this.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_NOZORDER);
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Create semi-transparent background for the form body
        using (Brush bodyBrush = new SolidBrush(Color.FromArgb(192, 0, 0, 0)))
        {
            e.Graphics.FillRectangle(bodyBrush, new Rectangle(0, 31, Width, Height));
        }

        // Create semi-transparent background for the title bar
        using (Brush titleBrush = new SolidBrush(Color.DarkBlue))
        {
            e.Graphics.FillRectangle(titleBrush, new Rectangle(0, 0, Width, 30)); // Adjust height as needed
        }
    }

    private void TitleLabel_DoubleClick(object? sender, EventArgs e)
    {
        // Allow editing of the title label on double click
        TextBox textBox = new TextBox
        {
            Text = titleLabel.Text,
            Bounds = titleLabel.Bounds, // Position the textbox
            BackColor = SystemColors.Window, // Match the system background color
            BorderStyle = BorderStyle.FixedSingle, // No border
            Font = titleLabel.Font, // Match the font
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

    private Rectangle GetWindowBounds()
    {
        // Calculate and set window size based on icon size and spacing
        int iconWidth, iconHeight, iconSpacingHorizontal, iconSpacingVertical;
        GetDesktopIconMetrics(out iconWidth, out iconHeight, out iconSpacingHorizontal, out iconSpacingVertical);

        // Calculate window size for 3 rows and 5 columns of icons
        int windowWidth = 5 * iconSpacingHorizontal;
        int windowHeight = 3 * iconSpacingVertical + HEADER_HEIGHT; // Add height for title bar

        // Set the initial size and position of the window
        Width = windowWidth;
        Height = windowHeight;

        // Get screen dimensions
        var screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        var screenHeight = Screen.PrimaryScreen!.Bounds.Height;

        // Calculate position (right side aligned, bottom 100px above the screen)
        int x = screenWidth - Width - 10;  // Position x such that right side touches viewport (with 10px padding)
        int y = screenHeight - Height - 100;  // Position y 100px above bottom of screen

        return new Rectangle(x, y, Width, Height);
    }

    // Method to get desktop icon metrics (size and spacing)
    private void GetDesktopIconMetrics(out int iconWidth, out int iconHeight, out int spacingHorizontal,
        out int spacingVertical)
    {
        Type t = typeof(SystemInformation);
        PropertyInfo[] pi = t.GetProperties();

        object? iconSizeObject = pi.FirstOrDefault(p => p.Name == "IconSize")?.GetValue(null);
        object? iconSpacingVObject = pi.FirstOrDefault(p => p.Name == "IconVerticalSpacing")?.GetValue(null);
        object? iconSpacingHObject = pi.FirstOrDefault(p => p.Name == "IconHorizontalSpacing")?.GetValue(null);

        var iconSize = (iconSizeObject != null) ? (Size)iconSizeObject : new Size(48, 48);
        var iconSpacingV = (iconSpacingVObject != null) ? (int)iconSpacingVObject : 75;
        var iconSpacingH = (iconSpacingHObject != null) ? (int)iconSpacingHObject : 75;

        iconWidth = iconSize.Width;
        iconHeight = iconSize.Height;
        spacingHorizontal = iconSpacingH;   // Size Of The Grid Holding Icon
        spacingVertical = iconSpacingV;     // Size Of The Grid Holding Icon
    }

    private Panel CreateHeaderPanel()
    {
        return new Panel
        {
            Text = title,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = HEADER_HEIGHT,
            Cursor = Cursors.SizeAll // Change cursor to hand over title bar
        };
    }

    private Label CreateTitleLabel()
    {
        return new Label
        {
            Text = title,
            AutoSize = true,
            Height = HEADER_HEIGHT,
            ForeColor = Color.Black,
            Cursor = Cursors.Default,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Tahoma", 16)
        };
    }

    private void SetWindowAppearance()
    {
        var bounds = GetWindowBounds(); // Get bounds for the window

        // Set window properties
        FormBorderStyle = FormBorderStyle.Sizable;
        ControlBox = false; // No control box
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        ShowInTaskbar = false; // Don't show in taskbar
        Opacity = 0.5;
    }

}