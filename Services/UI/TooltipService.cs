using Microsoft.AspNetCore.Components.Web;

public class TooltipService
{
    public string? Text { get; private set; }
    public double Top { get; private set; }
    public double Left {get; private set; }

    public event Action? OnChange;

    public void Show(MouseEventArgs e, string text)
    {
        Text = text;
        Top = e.ClientY; // vertical alignment
        Left = e.ClientX + 16; // horizontal offset from cursor
        OnChange?.Invoke();
    }

    public void ShowAt(double top, double left, string text)
    {
        Text = text;
        Top = top;
        Left = left;
        OnChange?.Invoke();
    }

    public void Hide()
    {
        Text = null;
        OnChange?.Invoke();
    }

}