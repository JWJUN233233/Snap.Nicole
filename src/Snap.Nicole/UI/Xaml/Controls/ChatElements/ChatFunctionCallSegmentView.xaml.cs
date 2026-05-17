using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.Services.AI.Observables;
using System.ComponentModel;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

[GeneratedDependencyProperty<ObservableFunctionCallContent>("FunctionCall", PropertyChangedCallbackName = nameof(OnFunctionCallChanged))]
internal sealed partial class ChatFunctionCallSegmentView : UserControl
{
    private ObservableFunctionCallContent? subscribedFunctionCall;
    private bool isLoaded;

    public ChatFunctionCallSegmentView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateText();
    }

    private static void OnFunctionCallChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ChatFunctionCallSegmentView view)
        {
            return;
        }

        view.UpdateFunctionCallSubscription();
        view.UpdateText();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        isLoaded = true;
        UpdateFunctionCallSubscription();
        UpdateText();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        isLoaded = false;
        UnsubscribeFunctionCall();
    }

    private void UpdateFunctionCallSubscription()
    {
        if (!isLoaded)
        {
            UnsubscribeFunctionCall();
            return;
        }

        if (ReferenceEquals(subscribedFunctionCall, FunctionCall))
        {
            return;
        }

        UnsubscribeFunctionCall();

        if (FunctionCall is not null)
        {
            subscribedFunctionCall = FunctionCall;
            subscribedFunctionCall.PropertyChanged += OnFunctionCallPropertyChanged;
        }
    }

    private void UnsubscribeFunctionCall()
    {
        if (subscribedFunctionCall is null)
        {
            return;
        }

        subscribedFunctionCall.PropertyChanged -= OnFunctionCallPropertyChanged;
        subscribedFunctionCall = null;
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
