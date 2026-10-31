using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components;

namespace DietTracker.Web.Pages;

public partial class Postavke
{
    [Inject] private AuthService Auth { get; set; } = default!;
    [Inject] private ThemeService Theme { get; set; } = default!;

    private static readonly (string Value, string Label)[] Options =
        [("system", "Sustav"), ("light", "Svijetla"), ("dark", "Tamna")];

    private string theme = "system";

    protected override async Task OnInitializedAsync() => theme = await Theme.GetAsync();

    private async Task SetTheme(string value)
    {
        theme = value;
        await Theme.SetAsync(value);
    }
}
