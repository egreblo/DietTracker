using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components;

namespace DietTracker.Web.Pages;

public partial class Login
{
    [Inject] private AuthService Auth { get; set; } = default!;
    [Inject] private SupabaseSettings Settings { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string email = "";
    private string password = "";
    private string? error;
    private bool busy;

    protected override void OnInitialized()
    {
        if (Auth.IsSignedIn) Nav.NavigateTo("", replace: true);
    }

    private async Task SubmitAsync()
    {
        busy = true;
        error = null;
        var (ok, message) = await Auth.SignInAsync(email, password);
        busy = false;
        if (ok) Nav.NavigateTo("", replace: true);
        else error = message;
    }
}
