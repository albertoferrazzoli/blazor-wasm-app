using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using blazor_wasm_app;
using blazor_wasm_app.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register repositories
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<IOrderRepository, OrderRepository>();

// Register services
builder.Services.AddSingleton<IReflectionService, ReflectionService>();
builder.Services.AddSingleton<IDynamicQueryService, DynamicQueryService>();
builder.Services.AddSingleton<AppStateService>();

await builder.Build().RunAsync();
