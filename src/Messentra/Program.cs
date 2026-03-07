using ElectronNET.API;
using ElectronNET.API.Entities;
using FluentValidation;
using Fluxor;
using Fluxor.Blazor.Web.ReduxDevTools;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using App = Messentra.Features.App;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddElectron();
builder.Services.AddMudServices();
builder.Services.AddFluxor(x =>
{
    x.ScanAssemblies(typeof(Program).Assembly);
    
#if DEBUG
    x.UseReduxDevTools();
#endif
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
    var version = typeof(Program).Assembly.GetName().Version;
    var versionString = version is not null ? $" v{version.Major}.{version.Minor}.{version.Build}" : string.Empty;
    var options = new BrowserWindowOptions 
    {
        Title = $"Messentra {versionString}",
        Show = false,
        IsRunningBlazor = true,
        MinHeight = 768,
        MinWidth = 1024,
        Width =  1024,
        Height = 768,
        WebPreferences = new WebPreferences
        {
            NodeIntegration = false,
            ContextIsolation = true,
#if !DEBUG
            DevTools = false
#endif
        }
    };
    
    if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        options.AutoHideMenuBar = true;
    
    var browserWindow = await Electron.WindowManager.CreateWindowAsync(options);
#if !DEBUG
    Electron.Menu.SetApplicationMenu([]);
#endif
#if DEBUG
    var extensionPath = builder.Configuration["ReduxDevTools:ExtensionPath"];
    
    await browserWindow.WebContents.Session.LoadExtensionAsync(extensionPath);
#endif
    browserWindow.OnReadyToShow += () => browserWindow.Show();
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

app.Run();