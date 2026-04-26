using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Concurrent;

namespace Snap.Nicole.UI.Xaml.Media.Backdrop;

// https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/main/src/controls/dev/Materials/DesktopAcrylicBackdrop/DesktopAcrylicBackdrop.cpp
internal sealed partial class InputActiveDesktopAcrylicBackdrop : SystemBackdrop
{
    private readonly ConcurrentDictionary<ICompositionSupportsSystemBackdrop, DesktopAcrylicController> controllers = [];

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(target, xamlRoot);

        SystemBackdropConfiguration configuration = GetDefaultSystemBackdropConfiguration(target, xamlRoot);
        configuration.IsInputActive = true;

        DesktopAcrylicController newController = new();
        newController.AddSystemBackdropTarget(target);
        newController.SetSystemBackdropConfiguration(configuration);
        controllers.TryAdd(target, newController);
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
    {
        base.OnTargetDisconnected(target);

        if (controllers.TryRemove(target, out DesktopAcrylicController? controller))
        {
            controller.RemoveSystemBackdropTarget(target);
            controller.Dispose();
        }
    }
}
