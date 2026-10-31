namespace DietTracker.Web.Services;

public record SupabaseSettings(string Url, string AnonKey)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(AnonKey);
}

public class SupabaseHttp
{
    public HttpClient Client { get; }

    public SupabaseHttp(SupabaseSettings settings)
    {
        Client = new HttpClient
        {
            BaseAddress = new Uri(settings.IsConfigured ? settings.Url.TrimEnd('/') + "/" : "http://localhost/"),
        };
        if (settings.IsConfigured)
            Client.DefaultRequestHeaders.Add("apikey", settings.AnonKey);
    }
}
