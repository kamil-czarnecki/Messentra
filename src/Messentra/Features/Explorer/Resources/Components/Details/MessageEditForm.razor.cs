using Messentra.Features.Explorer.Messages.SendMessage;
using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components.Details;

public sealed partial class MessageEditForm
{
    [Parameter, EditorRequired]
    public required MessageEditModel Model { get; set; }

    private void AddCustomProperty() =>
        Model.CustomProperties.Add(new CustomProperty());

    private void RemoveCustomProperty(CustomProperty property) =>
        Model.CustomProperties.Remove(property);
}
