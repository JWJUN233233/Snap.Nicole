using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.Services.AI.Observables;
using System.ComponentModel;
using System.Text.Json;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

internal sealed partial class ChatFunctionCallSegmentView : UserControl
{
    public ChatFunctionCallSegmentView()
    {
        InitializeComponent();
        UpdateText();
    }

    public ObservableFunctionCallContent? FunctionCall
    {
        get => (ObservableFunctionCallContent?)GetValue(FunctionCallProperty);
        set => SetValue(FunctionCallProperty, value);
    }

    public static readonly DependencyProperty FunctionCallProperty = DependencyProperty.Register(
        nameof(FunctionCall),
        typeof(ObservableFunctionCallContent),
        typeof(ChatFunctionCallSegmentView),
        new PropertyMetadata(null, OnFunctionCallChanged));

    private static void OnFunctionCallChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ChatFunctionCallSegmentView view)
        {
            return;
        }

        if (e.OldValue is ObservableFunctionCallContent oldFunctionCall)
        {
            oldFunctionCall.PropertyChanged -= view.OnFunctionCallPropertyChanged;
        }

        if (e.NewValue is ObservableFunctionCallContent newFunctionCall)
        {
            newFunctionCall.PropertyChanged += view.OnFunctionCallPropertyChanged;
        }

        view.UpdateText();
    }

    private void OnFunctionCallPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateText();
    }

    private void UpdateText()
    {
        Segment.Text = FunctionCall is null
            ? string.Empty
            : $"{FunctionCall.Name}: {Serialize(FunctionCall.Arguments)}";
    }

    private static string Serialize(object? value)
    {
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return value?.ToString() ?? string.Empty;
        }
    }
}
