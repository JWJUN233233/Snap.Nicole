using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Snap.Nicole.Services.AI.Observables;
using Snap.Nicole.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using VirtualKey = Windows.System.VirtualKey;

namespace Snap.Nicole.UI.Xaml.Pages;

internal sealed partial class ChatPage : Page
{
    public ChatPage()
    {
        InitializeComponent();
        ViewModel = App.Host.Services.GetRequiredService<ChatViewModel>();
        DataContext = ViewModel;

        ViewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        SubscribeMessages(ViewModel.Messages);
        InputBox.PreviewKeyDown += OnInputBoxPreviewKeyDown;
    }

    internal ChatViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ScrollToBottom();
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ObservableChatMessage message in e.OldItems)
            {
                UnsubscribeMessage(message);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ObservableChatMessage message in e.NewItems)
            {
                SubscribeMessage(message);
            }
        }

        ScrollToBottom();
    }

    private void OnMessageContentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ObservableAIContent content in e.OldItems)
            {
                content.PropertyChanged -= OnContentPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ObservableAIContent content in e.NewItems)
            {
                content.PropertyChanged += OnContentPropertyChanged;
            }
        }

        ScrollToBottom();
    }

    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ScrollToBottom();
    }

    private void OnContentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ChatScrollViewer.ChangeView(null, ChatScrollViewer.ScrollableHeight, null);
        });
    }

    private void SubscribeMessages(ObservableChatMessageCollection messages)
    {
        foreach (ObservableChatMessage message in messages)
        {
            SubscribeMessage(message);
        }
    }

    private void SubscribeMessage(ObservableChatMessage message)
    {
        message.PropertyChanged += OnMessagePropertyChanged;
        message.Contents.CollectionChanged += OnMessageContentsCollectionChanged;
        SubscribeContents(message.Contents);
    }

    private void UnsubscribeMessage(ObservableChatMessage message)
    {
        message.PropertyChanged -= OnMessagePropertyChanged;
        message.Contents.CollectionChanged -= OnMessageContentsCollectionChanged;
        UnsubscribeContents(message.Contents);
    }

    private void SubscribeContents(ObservableAIContentCollection contents)
    {
        foreach (ObservableAIContent content in contents)
        {
            content.PropertyChanged += OnContentPropertyChanged;
        }
    }

    private void UnsubscribeContents(ObservableAIContentCollection contents)
    {
        foreach (ObservableAIContent content in contents)
        {
            content.PropertyChanged -= OnContentPropertyChanged;
        }
    }

    private void OnInputBoxPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
        {
            return;
        }

        if (IsKeyDown(VirtualKey.Shift) || IsKeyDown(VirtualKey.Control))
        {
            InsertInputBoxLineBreak();
            e.Handled = true;
            return;
        }

        ViewModel.SendMessageCommand.Execute(null);
        e.Handled = true;
    }

    private void InsertInputBoxLineBreak()
    {
        string text = InputBox.Text ?? string.Empty;
        int selectionStart = InputBox.SelectionStart;
        int selectionLength = InputBox.SelectionLength;
        string newLine = Environment.NewLine;

        InputBox.Text = text[..selectionStart] + newLine + text[(selectionStart + selectionLength)..];
        InputBox.SelectionStart = selectionStart + newLine.Length;
        InputBox.SelectionLength = 0;
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        return (global::Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key) & global::Windows.UI.Core.CoreVirtualKeyStates.Down) == global::Windows.UI.Core.CoreVirtualKeyStates.Down;
    }
}
