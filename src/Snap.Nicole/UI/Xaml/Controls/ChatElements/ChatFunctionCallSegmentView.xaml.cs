using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.Services.AI.Observables;
using System.ComponentModel;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

[GeneratedDependencyProperty<ObservableFunctionCallContent>("FunctionCall", PropertyChangedCallbackName = nameof(OnFunctionCallChanged))]
internal sealed partial class ChatFunctionCallSegmentView : UserControl
{
    public ChatFunctionCallSegmentView()
    {
        InitializeComponent();
        UpdateText();
    }

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
            : $"{FunctionCall.Name}: {FunctionCall.Arguments}";
    }
}
