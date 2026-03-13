using ElectronNET.API;
using ElectronNET.API.Entities;
using FluentValidation;
using Fluxor;
using Fluxor.Blazor.Web.ReduxDevTools;
using Messentra;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using App = Messentra.Features.App;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrEmpty(builder.Environment.WebRootPath))
    builder.Environment.WebRootPath = Path.Combine(AppContext.BaseDirectory, "publish", "bin", "wwwroot");

LoggingConfiguration.ConfigureLogging(builder.Host);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddElectron();
builder.Services.AddMudServices();
builder.Services.AddFluxor(x =>
{
    x.ScanAssemblies(typeof(Program).Assembly);

    if (builder.Environment.IsDevelopment())
        x.UseReduxDevTools();
});
builder.Services.AddMediator(opts =>
{
    opts.ServiceLifetime = ServiceLifetime.Scoped;
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Database
builder.Services.AddDbContext<MessentraDbContext>();

// Services
builder.Services.AddInfrastructure();
builder.Services.AddScoped<ResourceSelector>();
builder.UseElectron(args, async () =>
{
    // Splash screen
    var splashOptions = new BrowserWindowOptions
    {
        Width = 400,
        Height = 300,
        Frame = false,
        Movable = false,
        AlwaysOnTop = true,
        Show = true,
        IsRunningBlazor = false
    };
    var splash = await Electron.WindowManager.CreateWindowAsync(splashOptions);
    var splashUrl = new Uri(Path.Combine(builder.Environment.WebRootPath, "splash.html")).AbsoluteUri;
    splash.LoadURL(splashUrl);

    // Main window
    var version = typeof(Program).Assembly.GetName().Version;
    var versionString = version is not null ? $" v{version.Major}.{version.Minor}.{version.Build}" : string.Empty;
    var options = new BrowserWindowOptions
    {
        Title = $"Messentra {versionString}",
        Show = false,
        IsRunningBlazor = true,
        MinHeight = 768,
        MinWidth = 1024,
        Width = 1024,
        Height = 768,
        WebPreferences = new WebPreferences
        {
            NodeIntegration = false,
            ContextIsolation = true,
            DevTools = builder.Environment.IsDevelopment()
        }
    };

    if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        options.AutoHideMenuBar = true;

    if (!builder.Environment.IsDevelopment())
        ElectronMenu.CreateApplicationMenu();
    
    var browserWindow = await Electron.WindowManager.CreateWindowAsync(options);

    if (builder.Environment.IsDevelopment())
    {
        var extensionPath = builder.Configuration["ReduxDevTools:ExtensionPath"];
        if (extensionPath is not null)
            await browserWindow.WebContents.Session.LoadExtensionAsync(extensionPath);
    }

    browserWindow.OnReadyToShow += () =>
    {
        splash.Close();
        browserWindow.Show();
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MessentraDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}