using System.Net.Http.Json;
using System.Text.Json;
using DietTracker.Core;
using Microsoft.JSInterop;

namespace DietTracker.Web.Services;

/// <summary>Email + password sign-in against Supabase Auth (GoTrue). Session is kept in localStorage.</summary>
public class AuthService(SupabaseHttp http, IJSRuntime js)
{
    private const string StorageKey = "dt.session";
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private StoredSession? _session;

    public bool IsSignedIn => _session is not null;
    public string? Email => _session?.Email;
    public event Action? Changed;

    private record StoredSession(string AccessToken, string RefreshToken, long ExpiresAt, string? Email);

    private class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public long ExpiresAt { get; set; }
        public int ExpiresIn { get; set; }
        public UserInfo? User { get; set; }
    }

    private class UserInfo
    {
        public string? Email { get; set; }
    }

    private class ErrorResponse
    {
        public string? ErrorCode { get; set; }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var raw = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(raw))
                _session = JsonSerializer.Deserialize<StoredSession>(raw, Json.Options);
        }
        catch
        {
            _session = null;
        }

        if (_session is not null && IsExpiring(_session))
            await RefreshAsync();
    }

    public async Task<(bool Ok, string? Error)> SignInAsync(string email, string password)
    {
        try
        {
            using var res = await http.Client.PostAsJsonAsync(
                "auth/v1/token?grant_type=password", new { email = email.Trim(), password });

            if (!res.IsSuccessStatusCode)
            {
                var error = await TryReadError(res);
                return (false, error == "invalid_credentials"
                    ? "Pogrešan email ili lozinka."
                    : "Prijava nije uspjela. Pokušaj ponovno.");
            }

            var token = await res.Content.ReadFromJsonAsync<TokenResponse>(Json.Options);
            if (token is null) return (false, "Prijava nije uspjela. Pokušaj ponovno.");

            await StoreAsync(ToSession(token));
            return (true, null);
        }
        catch (HttpRequestException)
        {
            return (false, "Nema veze s poslužiteljem.");
        }
    }

    public async Task SignOutAsync()
    {
        _session = null;
        try { await js.InvokeVoidAsync("localStorage.removeItem", StorageKey); } catch { /* storage unavailable */ }
        Changed?.Invoke();
    }

    /// <summary>Returns a valid access token, refreshing it first when it is about to expire.</summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        if (_session is null) return null;
        if (IsExpiring(_session)) await RefreshAsync();
        return _session?.AccessToken;
    }

    private async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var current = _session;
            if (current is null || !IsExpiring(current)) return; // another caller already refreshed

            using var res = await http.Client.PostAsJsonAsync(
                "auth/v1/token?grant_type=refresh_token", new { refresh_token = current.RefreshToken });

            if (res.IsSuccessStatusCode)
            {
                var token = await res.Content.ReadFromJsonAsync<TokenResponse>(Json.Options);
                if (token is not null) await StoreAsync(ToSession(token, current.Email));
            }
            else if ((int)res.StatusCode is 400 or 401 or 403)
            {
                await SignOutAsync(); // refresh token revoked or invalid
            }
        }
        catch (HttpRequestException)
        {
            // Offline: keep the session, the request itself will fail with a clearer message.
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static bool IsExpiring(StoredSession s) =>
        DateTimeOffset.FromUnixTimeSeconds(s.ExpiresAt) - DateTimeOffset.UtcNow < RefreshMargin;

    private static StoredSession ToSession(TokenResponse t, string? fallbackEmail = null) => new(
        t.AccessToken,
        t.RefreshToken,
        t.ExpiresAt > 0 ? t.ExpiresAt : DateTimeOffset.UtcNow.AddSeconds(t.ExpiresIn).ToUnixTimeSeconds(),
        t.User?.Email ?? fallbackEmail);

    private async Task StoreAsync(StoredSession session)
    {
        var wasSignedIn = IsSignedIn;
        _session = session;
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(session, Json.Options));
        }
        catch { /* storage unavailable: session lasts until reload */ }
        if (!wasSignedIn) Changed?.Invoke();
    }

    private static async Task<string?> TryReadError(HttpResponseMessage res)
    {
        try { return (await res.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))?.ErrorCode; }
        catch { return null; }
    }
}
