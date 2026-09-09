using Microsoft.AspNetCore.Components;

namespace OperationalIntelligenceHub.Layout;

public sealed class DiagnosticsConsolePortalState
{
    public RenderFragment? Content { get; private set; }

    public event Action? ContentChanged;

    public void SetContent(RenderFragment? content)
    {
        if (ReferenceEquals(Content, content))
        {
            return;
        }

        Content = content;
        ContentChanged?.Invoke();
    }

    public void ClearContent(RenderFragment? content)
    {
        if (!ReferenceEquals(Content, content))
        {
            return;
        }

        Content = null;
        ContentChanged?.Invoke();
    }
}
