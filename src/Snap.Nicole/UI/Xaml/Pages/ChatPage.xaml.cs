using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.UI.Xaml.Helpers;
using Snap.Nicole.ViewModels;
using System;
using System.Collections.Specialized;
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
        InputBox.KeyDown += OnInputBoxKeyDown;
    }

    internal ChatViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RebuildMessages();
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildMessages();
    }

    private void RebuildMessages()
    {
        MessagesPanel.Children.Clear();

        foreach (ExtendedAgentResponseUpdate message in ViewModel.Messages)
        {
            FrameworkElement bubble = MarkdownHelper.CreateMessageBubble(message);
            MessagesPanel.Children.Add(bubble);
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            ChatScrollViewer.ChangeView(null, ChatScrollViewer.ScrollableHeight, null);
        });
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
