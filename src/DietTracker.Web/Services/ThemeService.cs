using Microsoft.JSInterop;

namespace DietTracker.Web.Services;

/// <summary>Theme preference: "system" (default), "light" or "dark". Applied by wwwroot/js/app.js.</summary>
public class ThemeService(IJSRuntime js)
{
    public async Task<string> GetAsync() => await js.InvokeAsync<string>("dt.getTheme");

    public async Task SetAsync(string theme) => await js.InvokeVoidAsync("dt.setTheme", theme);

    /// <summary>Resolved theme actually on screen: "light" or "dark".</summary>
    public async Task<string> GetResolvedAsync() => await js.InvokeAsync<string>("dt.resolvedTheme");
}
