using MoonWorks.Graphics.Font;
using Tactician.Content;
using Tactician.Data;

namespace Tactician;

public struct Text {
    public FontID FontID { get; }
    public int Size { get; }
    public int TextID { get; }
    public HorizontalAlignment HorizontalAlignment { get; }
    public VerticalAlignment VerticalAlignment { get; }

    public Text(
        FontID packID,
        int size,
        string text,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment verticalAlignment = VerticalAlignment.Baseline
    ) {
        FontID = packID;
        Size = size;
        TextID = TextStorage.GetID(text);
        HorizontalAlignment = horizontalAlignment;
        VerticalAlignment = verticalAlignment;
    }

    public Text(
        FontID packID,
        int size,
        int textID,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment verticalAlignment = VerticalAlignment.Baseline
    ) {
        FontID = packID;
        Size = size;
        TextID = textID;
        HorizontalAlignment = horizontalAlignment;
        VerticalAlignment = verticalAlignment;
    }
}