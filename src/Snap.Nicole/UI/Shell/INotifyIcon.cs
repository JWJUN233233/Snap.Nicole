using Snap.Nicole.Native.Foundation;

namespace Snap.Nicole.UI.Shell;

internal interface INotifyIcon
{
    void Create();

    void Recreate();

    void RequestContextMenu(RECT iconRect, POINT cursorPos);

    void RequestMainWindow();
}
