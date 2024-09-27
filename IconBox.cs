using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

    private bool isDragging = false;
    private string title = "Icon Box Demo";
    private Point dragStartPoint;
    private Label titleLabel;
    private Panel headerPanel;

    public IconBox()
    {
        var bounds = GetWindowBounds(); // Get bounds for the window

        // Set window properties
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.ControlBox = false; // No control box
        this.StartPosition = FormStartPosition.Manual;
        this.Bounds = bounds;
        this.ShowInTaskbar = false; // Don't show in taskbar
        this.Opacity = 0.5;

        // Create The Title Label
        titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Height = 30,
            ForeColor = Color.Black,
            Cursor = Cursors.Default,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Tahoma", 16)
        };

        // Create Title Bar
        headerPanel = new Panel
        {
            Text = title,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 30,
            Cursor = Cursors.SizeAll // Change cursor to hand over title bar
        };

        titleLabel.DoubleClick += TitleLabel_DoubleClick!; // Double click to edit title

        headerPanel.Controls.Add(titleLabel);

        headerPanel.MouseMove += HeadePanel_MouseMove!; // Mouse move to enable dragging
        headerPanel.MouseDown += HeadePanel_MouseDown!; // Mouse down to start dragging
        headerPanel.MouseUp += HeadePanel_MouseUp!; // Mouse up to stop dragging

        this.Controls.Add(headerPanel);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Create semi-transparent background for the form body
        using (Brush bodyBrush = new SolidBrush(Color.FromArgb(192, 0, 0, 0)))
        {
            e.Graphics.FillRectangle(bodyBrush, new Rectangle(0, 31, this.Width, this.Height));
        }

        // Create semi-transparent background for the title bar
        using (Brush titleBrush = new SolidBrush(Color.DarkBlue))
        {
            e.Graphics.FillRectangle(titleBrush, new Rectangle(0, 0, this.Width, 30)); // Adjust height as needed
        }
    }

    private void TitleLabel_DoubleClick(object sender, EventArgs e)
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
        textBox.Leave += (s, args) =>
        {
            ConfirmTitleChange(textBox);
        };

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

    private void HeadePanel_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            // Move the window by adjusting its location
            this.Left += e.X - dragStartPoint.X;
            this.Top += e.Y - dragStartPoint.Y;
        }
    }

    private void HeadePanel_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isDragging = true;
            dragStartPoint = e.Location;
        }
    }

    private void HeadePanel_MouseUp(object sender, MouseEventArgs e)
    {
        isDragging = false;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        this.ShowInTaskbar = false; // Don't show in taskbar
    }

    private Rectangle GetWindowBounds()
    {
        // Set the initial size and position of the window
        this.Width = 300;
        this.Height = 300;

        // Get screen dimensions
        var screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        var screenHeight = Screen.PrimaryScreen!.Bounds.Height;

        // Calculate position (right side aligned, bottom 100px above the screen)
        int x = screenWidth - this.Width - 10;  // Position x such that right side touches viewport (with 10px padding)
        int y = screenHeight - this.Height - 100;  // Position y 100px above bottom of screen

        return new Rectangle(x, y, this.Width, this.Height);
    }
}
