using System.Runtime.InteropServices;

namespace iFence;

public class IconBox : Form
{
    // P/Invoke to get and set window styles
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private bool isDragging;
    private readonly string title = "Icon Box";
    private Point dragStartPoint;
    private readonly Label titleLabel;
    private readonly Panel headerPanel;

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

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ShowInTaskbar = false; // Don't show in taskbar
    }

    private Rectangle GetWindowBounds()
    {
        // Set the initial size and position of the window
        Width = 300;
        Height = 300;

        // Get screen dimensions
        var screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        var screenHeight = Screen.PrimaryScreen!.Bounds.Height;

        // Calculate position (right side aligned, bottom 100px above the screen)
        int x = screenWidth - Width - 10;  // Position x such that right side touches viewport (with 10px padding)
        int y = screenHeight - Height - 100;  // Position y 100px above bottom of screen

        return new Rectangle(x, y, Width, Height);
    }

    private Panel CreateHeaderPanel()
    {
        return new Panel
        {
            Text = title,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 30,
            Cursor = Cursors.SizeAll // Change cursor to hand over title bar
        };
    }

    private Label CreateTitleLabel()
    {
        return new Label
        {
            Text = title,
            AutoSize = true,
            Height = 30,
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