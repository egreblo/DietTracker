using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DietTracker.Core;

namespace DietTracker.Web.Services;

public class ApiException(string message) : Exception(message);

/// <summary>Thin PostgREST client: attaches the user's JWT and turns failures into <see cref="ApiException"/>.</summary>
public class SupabaseApi(SupabaseHttp http, AuthService auth, SupabaseSettings settings)
{
    public async Task<List<T>> GetAsync<T>(string path)
    {
        using var res = await SendAsync(HttpMethod.Get, path, null);
        return await res.Content.ReadFromJsonAsync<List<T>>(Json.Options) ?? [];
    }

    public async Task WriteAsync<T>(HttpMethod method, string path, T body, string prefer = "return=minimal")
    {
        using var res = await SendAsync(method, path, JsonContent.Create(body, options: Json.Options), prefer);
    }

    public async Task DeleteAsync(string path)
    {
        using var res = await SendAsync(HttpMethod.Delete, path, null);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, string? prefer = null)
    {
        var token = await auth.GetAccessTokenAsync();
        using var req = new HttpRequestMessage(method, path) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? settings.AnonKey);
        if (prefer is not null) req.Headers.Add("Prefer", prefer);

        HttpResponseMessage res;
        try
        {
            res = await http.Client.SendAsync(req);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("Nema veze s poslužiteljem.");
        }

        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            res.Dispose();
            await auth.SignOutAsync();
            throw new ApiException("Sesija je istekla. Prijavi se ponovno.");
        }
        if (!res.IsSuccessStatusCode)
        {
            var text = await res.Content.ReadAsStringAsync();
            res.Dispose();
            throw new ApiException($"Greška {(int)res.StatusCode}: {text}");
        }
        return res;
    }
}
