using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TMS.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Optional: allow configuring the API base URL from wwwroot/appsettings.json
builder.Configuration.AddJsonStream(await new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
    .GetStreamAsync("appsettings.json"));

// In development the Blazor app and API often run on different origins.
// Use an explicit API base address (fallback to the app base address).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var baseAddress = !string.IsNullOrWhiteSpace(apiBaseUrl)
    ? new Uri(apiBaseUrl, UriKind.Absolute)
    : new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = baseAddress });

await builder.Build().RunAsync();
