using System.Collections;

namespace IcoBox;

public enum ArrangeType
{
    ByName,
    ByType
}

public class ListViewItemComparer(ArrangeType arrangetype, bool ascending) : IComparer
{
    public int Compare(object? x, object? y)
    {
        ListViewItem? item1 = (ListViewItem?)x;
        ListViewItem? item2 = (ListViewItem?)y;

        // Assuming ItemName is in subitem 0 and ItemType is in subitem 1
        int result;

        if (arrangetype == ArrangeType.ByName) // Sort by ItemName
            result = string.Compare(item1?.Text, item2?.Text);
        else // Sort by ItemType
            result = string.Compare(item1?.GetType().Name, item2?.GetType().Name);

        // Return result based on ascending or descending order
        return ascending ? result : -result;
    }
}
