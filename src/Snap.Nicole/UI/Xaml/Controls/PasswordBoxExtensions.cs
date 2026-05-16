using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Snap.Nicole.UI.Xaml.Controls;

[GeneratedDependencyProperty<string>("Password", IsAttached = true, TargetType = typeof(PasswordBox), PropertyChangedCallbackName = nameof(OnPasswordChanged))]
public static partial class PasswordBoxExtensions
{
    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= OnPasswordBoxPasswordChanged;

        string newPassword = e.NewValue as string ?? string.Empty;
        if (!string.Equals(passwordBox.Password, newPassword))
        {
            passwordBox.Password = newPassword;
        }

        passwordBox.PasswordChanged += OnPasswordBoxPasswordChanged;
    }

    private static void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            SetPassword(passwordBox, passwordBox.Password);
        }
    }
}
