using System.Runtime.InteropServices;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace Messentra;

public static class ElectronMenu
{
    public static void CreateApplicationMenu(Func<Task>? onClearTokenCacheRequested = null)
    {
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var menu = new List<MenuItem>();

        if (isMac)
        {
            menu.Add(new MenuItem
            {
                Label = "Messentra",
                Submenu =
                [
                    new MenuItem { Role = MenuRole.about },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem { Role = MenuRole.services },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem { Role = MenuRole.hide },
                    new MenuItem { Role = MenuRole.hideothers },
                    new MenuItem { Role = MenuRole.unhide },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem { Role = MenuRole.quit }
                ]
            });
        }

        menu.Add(new MenuItem
        {
            Label = "File",
            Submenu =
            [
                new MenuItem { Role = MenuRole.close },
                new MenuItem { Type = MenuType.separator },
                new MenuItem
                {
                    Label = "Clear Token Cache",
                    Click = () =>
                    {
                        if (onClearTokenCacheRequested is null)
                            return;

                        _ = onClearTokenCacheRequested();
                    }
                },
                ..isMac
                    ? Array.Empty<MenuItem>()
                    : new[]
                    {
                        new MenuItem { Type = MenuType.separator },
                        new MenuItem { Role = MenuRole.quit }
                    }
            ]
        });

        menu.Add(new MenuItem
        {
            Label = "Edit",
            Submenu =
            [
                new MenuItem { Role = MenuRole.undo },
                new MenuItem { Role = MenuRole.redo },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Role = MenuRole.cut },
                new MenuItem { Role = MenuRole.copy },
                new MenuItem { Role = MenuRole.paste },
                new MenuItem { Role = MenuRole.delete },
                new MenuItem { Role = MenuRole.selectall }
            ]
        });

        menu.Add(new MenuItem
        {
            Label = "View",
            Submenu =
            [
                new MenuItem { Role = MenuRole.resetzoom },
                new MenuItem { Role = MenuRole.zoomin },
                new MenuItem { Role = MenuRole.zoomout },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Role = MenuRole.togglefullscreen }
            ]
        });

        menu.Add(new MenuItem
        {
            Label = "Window",
            Submenu = isMac
                ?
                [
                    new MenuItem { Role = MenuRole.minimize },
                    new MenuItem { Role = MenuRole.zoom },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem { Role = MenuRole.front }
                ]
                :
                [
                    new MenuItem { Role = MenuRole.minimize },
                    new MenuItem { Role = MenuRole.close }
                ]
        });

        menu.Add(new MenuItem
        {
            Label = "Help",
            Submenu =
            [
                new MenuItem
                {
                    Label = "Github",
                    Click = () => { Electron.Shell.OpenExternalAsync("https://github.com/kamil-czarnecki/Messentra"); }
                },
                new MenuItem
                {
                    Label = "Report Issue",
                    Click = () =>
                    {
                        Electron.Shell.OpenExternalAsync("https://github.com/kamil-czarnecki/Messentra/issues");
                    }
                }
            ]
        });

        Electron.Menu.SetApplicationMenu(menu.ToArray());
    }
}