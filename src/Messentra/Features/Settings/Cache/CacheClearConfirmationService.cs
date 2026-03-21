using ElectronNET.API;
using ElectronNET.API.Entities;

namespace Messentra.Features.Settings.Cache;

public interface ICacheClearConfirmationService
{
    Task<bool> ConfirmClearAsync(CancellationToken cancellationToken);
}

public sealed class CacheClearConfirmationService : ICacheClearConfirmationService
{
    public async Task<bool> ConfirmClearAsync(CancellationToken cancellationToken)
    {
        var result = await Electron.Dialog.ShowMessageBoxAsync(
            new MessageBoxOptions("Are you sure you want to clear the token cache and restart the application?")
            {
                Title = "Clear token cache",
                Type = MessageBoxType.question,
                Buttons = ["Yes", "No"],
                DefaultId = 1,
                CancelId = 1
            }).ConfigureAwait(false);

        return result.Response == 0;
    }
}