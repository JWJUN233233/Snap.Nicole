using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.Services.AI.Observables;
using System.ComponentModel;
using System.Text.Json;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

internal sealed partial class ChatFunctionResultSegmentView : UserControl
{
    private ObservableFunctionResultContent? subscribedFunctionResult;
    private bool isLoaded;

    public ChatFunctionResultSegmentView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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

        view.UpdateFunctionResultSubscription();
        view.UpdateText();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        isLoaded = true;
        UpdateFunctionResultSubscription();
        UpdateText();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        isLoaded = false;
        UnsubscribeFunctionResult();
    }

    private void UpdateFunctionResultSubscription()
    {
        if (!isLoaded)
        {
            UnsubscribeFunctionResult();
            return;
        }

        if (ReferenceEquals(subscribedFunctionResult, FunctionResult))
        {
            return;
        }

        UnsubscribeFunctionResult();

        if (FunctionResult is not null)
        {
            subscribedFunctionResult = FunctionResult;
            subscribedFunctionResult.PropertyChanged += OnFunctionResultPropertyChanged;
        }
    }

    private void UnsubscribeFunctionResult()
    {
        if (subscribedFunctionResult is null)
        {
            return;
        }

        subscribedFunctionResult.PropertyChanged -= OnFunctionResultPropertyChanged;
        subscribedFunctionResult = null;
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
