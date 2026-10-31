using DietTracker.Web;
using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(new SupabaseSettings(
    builder.Configuration["Supabase:Url"] ?? "",
    builder.Configuration["Supabase:AnonKey"] ?? ""));
builder.Services.AddSingleton<SupabaseHttp>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<SupabaseApi>();
builder.Services.AddSingleton<RecipeService>();
builder.Services.AddSingleton<PlanService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<Loc>();

var host = builder.Build();
await host.Services.GetRequiredService<AuthService>().InitializeAsync();
await host.RunAsync();
