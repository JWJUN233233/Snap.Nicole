namespace Snap.Nicole.UI.Xaml;

internal interface IXamlWindowCloseHandler
{
    void OnWindowClosing(out bool cancel);

    void OnWindowClosed()
    {
    }
}
