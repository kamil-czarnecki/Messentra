using System.Diagnostics.CodeAnalysis;
using ElectronNET.API;

namespace Messentra;

public interface IApplicationLifecycleService
{
    void Relaunch();
    void Exit();
}

[ExcludeFromCodeCoverage]
public sealed class ApplicationLifecycleService : IApplicationLifecycleService
{
    public void Relaunch() => Electron.App.Relaunch();

    public void Exit() => Electron.App.Exit();
}