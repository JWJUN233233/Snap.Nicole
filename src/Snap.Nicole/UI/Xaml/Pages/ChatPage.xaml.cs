using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Snap.Nicole.Services.AI.Observables;
using Snap.Nicole.ViewModels;
using System;
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
        InputBox.KeyDown += OnInputBoxKeyDown;
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

    private void OnInputBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            bool isShiftDown = (global::Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & global::Windows.UI.Core.CoreVirtualKeyStates.Down) == global::Windows.UI.Core.CoreVirtualKeyStates.Down;
            if (!isShiftDown)
            {
                ViewModel.SendMessageCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
