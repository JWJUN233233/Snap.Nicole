using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Snap.Nicole.UI.Xaml.Controls;

public static class PasswordBoxExtensions
{
    public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
        "Password",
        typeof(string),
        typeof(PasswordBoxExtensions),
        new PropertyMetadata(null, OnPasswordChanged));

    public static string? GetPassword(PasswordBox passwordBox)
    {
        ArgumentNullException.ThrowIfNull(passwordBox);

        return (string?)passwordBox.GetValue(PasswordProperty);
    }

    public static void SetPassword(PasswordBox passwordBox, string? value)
    {
        ArgumentNullException.ThrowIfNull(passwordBox);

        passwordBox.SetValue(PasswordProperty, value);
    }

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= OnPasswordBoxPasswordChanged;

        string password = e.NewValue as string ?? string.Empty;
        if (passwordBox.Password != password)
        {
            passwordBox.Password = password;
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
