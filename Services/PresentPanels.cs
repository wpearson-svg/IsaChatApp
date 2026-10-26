namespace IsaChatApp.Services;

/// <summary>
/// Lets Isa's Backstage menu open the presentation's own panels (Developer, Content Health) while the
/// /present slides are showing, instead of leaving the page. One per browser connection (scoped).
/// </summary>
public sealed class PresentPanels
{
    /// <summary>True while /present is open in this connection.</summary>
    public bool Active { get; set; }

    /// <summary>Raised with a dashboard's path ("/dev-dashboard", "/content-health-dashboard").</summary>
    public event Func<string, Task>? Requested;

    public Task RequestAsync(string path) => Requested?.Invoke(path) ?? Task.CompletedTask;
}
