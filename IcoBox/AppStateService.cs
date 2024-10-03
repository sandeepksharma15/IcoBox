using System.Diagnostics;
using System.Text.Json;

namespace IcoBox;

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

internal interface IAppStateService
{
    void SaveAppState();
    void RestoreAppState();
}

internal class AppStateService : IAppStateService
{
    private static string GetSaveFile
        => Path.Combine(IconBox.AppFolder!, AppInfo.SaveFileName);

    public void SaveAppState()
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

    public void RestoreAppState()
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
}
