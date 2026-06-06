using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Runtime.CompilerServices;

namespace Snap.Nicole.UI.Xaml;

[GeneratedDependencyProperty<InputSystemCursorShape>("Cursor", IsAttached = true, NotNull = true, PropertyChangedCallbackName = nameof(OnCursorChanged))]
public static partial class UIElementExtensions
{
    private static void OnCursorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            SetProtectedCursor(element, InputSystemCursor.Create((InputSystemCursorShape)e.NewValue));
        }
    }

    // protected InputCursor ProtectedCursor { get; set; }
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ProtectedCursor")]
    private static extern void SetProtectedCursor(UIElement uiElement, InputCursor cursor);
}