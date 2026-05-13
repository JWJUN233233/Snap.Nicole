using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.Services.AI.Observables;
using System.ComponentModel;
using System.Text.Json;

namespace Snap.Nicole.UI.Xaml.Controls;

internal sealed partial class ChatFunctionResultSegmentView : UserControl
{
    public ChatFunctionResultSegmentView()
    {
        InitializeComponent();
        UpdateText();
    }

    public ObservableFunctionResultContent? FunctionResult
    {
        get => (ObservableFunctionResultContent?)GetValue(FunctionResultProperty);
        set => SetValue(FunctionResultProperty, value);
    }

    public static readonly DependencyProperty FunctionResultProperty = DependencyProperty.Register(
        nameof(FunctionResult),
        typeof(ObservableFunctionResultContent),
        typeof(ChatFunctionResultSegmentView),
        new PropertyMetadata(null, OnFunctionResultChanged));

    private static void OnFunctionResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ChatFunctionResultSegmentView view)
        {
            return;
        }

        if (e.OldValue is ObservableFunctionResultContent oldFunctionResult)
        {
            oldFunctionResult.PropertyChanged -= view.OnFunctionResultPropertyChanged;
        }

        if (e.NewValue is ObservableFunctionResultContent newFunctionResult)
        {
            newFunctionResult.PropertyChanged += view.OnFunctionResultPropertyChanged;
        }

        view.UpdateText();
    }

    private void OnFunctionResultPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateText();
    }

    private void UpdateText()
    {
        Segment.Text = FunctionResult is null
            ? string.Empty
            : Serialize(FunctionResult.Result);
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
