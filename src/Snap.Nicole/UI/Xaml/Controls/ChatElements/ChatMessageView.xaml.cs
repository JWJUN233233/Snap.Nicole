using Microsoft.Extensions.AI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Observables;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

internal sealed partial class ChatMessageView : UserControl
{
    private ObservableAIContentCollection? subscribedContents;

    public ChatMessageView()
    {
        InitializeComponent();
        UpdateView();
    }

    public ObservableChatMessage? Message
    {
        get => (ObservableChatMessage?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(ObservableChatMessage),
        typeof(ChatMessageView),
        new PropertyMetadata(null, OnMessageChanged));

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ChatMessageView view)
        {
            return;
        }

        if (e.OldValue is ObservableChatMessage oldMessage)
        {
            view.UnsubscribeMessage(oldMessage);
        }

        if (e.NewValue is ObservableChatMessage newMessage)
        {
            view.SubscribeMessage(newMessage);
        }

        view.UpdateView();
    }

    private void SubscribeMessage(ObservableChatMessage message)
    {
        message.PropertyChanged += OnMessagePropertyChanged;
        SubscribeContents(message.Contents);
    }

    private void UnsubscribeMessage(ObservableChatMessage message)
    {
        message.PropertyChanged -= OnMessagePropertyChanged;
        UnsubscribeContents();
    }

    private void SubscribeContents(ObservableAIContentCollection contents)
    {
        subscribedContents = contents;
        subscribedContents.CollectionChanged += OnContentsCollectionChanged;

        foreach (ObservableAIContent content in subscribedContents)
        {
            content.PropertyChanged += OnContentPropertyChanged;
        }
    }

    private void UnsubscribeContents()
    {
        if (subscribedContents is null)
        {
            return;
        }

        subscribedContents.CollectionChanged -= OnContentsCollectionChanged;

        foreach (ObservableAIContent content in subscribedContents)
        {
            content.PropertyChanged -= OnContentPropertyChanged;
        }

        subscribedContents = null;
    }

    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObservableChatMessage.Contents) && Message is not null)
        {
            UnsubscribeContents();
            SubscribeContents(Message.Contents);
        }

        UpdateView();
    }

    private void OnContentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

        UpdateView();
    }

    private void OnContentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Message?.Role == ChatRole.User)
        {
            UpdateUserMessageText();
        }
    }

    private void UpdateView()
    {
        if (Message is null)
        {
            MessageBorder.Visibility = Visibility.Collapsed;
            ContentItemsControl.ItemsSource = null;
            UserMessageTextBlock.Text = string.Empty;
            SetHeaderText(string.Empty);
            return;
        }

        MessageBorder.Visibility = Visibility.Visible;

        if (Message.Role == ChatRole.User)
        {
            SetHeaderResourceText(SRName.UIXamlControlsChatMessageViewLabelUser);
        }
        else if (string.IsNullOrWhiteSpace(Message.AuthorName))
        {
            SetHeaderResourceText(SRName.UIXamlControlsChatMessageViewLabelAssistant);
        }
        else
        {
            SetHeaderText(Message.AuthorName);
        }

        bool isUserMessage = Message.Role == ChatRole.User;
        UserMessageTextBlock.Visibility = isUserMessage ? Visibility.Visible : Visibility.Collapsed;
        ContentItemsControl.Visibility = isUserMessage ? Visibility.Collapsed : Visibility.Visible;
        ContentItemsControl.ItemsSource = isUserMessage ? null : Message.Contents;

        if (isUserMessage)
        {
            UpdateUserMessageText();
        }
        else
        {
            UserMessageTextBlock.Text = string.Empty;
        }
    }

    private void UpdateUserMessageText()
    {
        UserMessageTextBlock.Text = Message is null
            ? string.Empty
            : string.Concat(Message.Contents.OfType<ObservableTextContent>().Select(content => content.Text));
    }

    private void SetHeaderResourceText(SRName resourceName)
    {
        HeaderTextBlock.SetBinding(TextBlock.TextProperty, new Binding
        {
            Source = StringResourceProxy.Default,
            Path = new PropertyPath($"[{resourceName}]"),
            Mode = BindingMode.OneWay,
        });
    }

    private void SetHeaderText(string text)
    {
        HeaderTextBlock.ClearValue(TextBlock.TextProperty);
        HeaderTextBlock.Text = text;
    }
}
