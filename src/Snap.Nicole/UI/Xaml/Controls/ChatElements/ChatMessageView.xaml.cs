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

[GeneratedDependencyProperty<ObservableChatMessage>("Message", PropertyChangedCallbackName = nameof(OnMessageChanged))]
internal sealed partial class ChatMessageView : UserControl
{
    private ObservableChatMessage? subscribedMessage;
    private ObservableAIContentCollection? subscribedContents;
    private bool isLoaded;

    public ChatMessageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateView();
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ChatMessageView view)
        {
            return;
        }

        view.UpdateMessageSubscription();
        view.UpdateView();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        isLoaded = true;
        UpdateMessageSubscription();
        UpdateView();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        isLoaded = false;
        UnsubscribeMessage();
    }

    private void UpdateMessageSubscription()
    {
        if (!isLoaded)
        {
            UnsubscribeMessage();
            return;
        }

        if (ReferenceEquals(subscribedMessage, Message))
        {
            return;
        }

        UnsubscribeMessage();

        if (Message is ObservableChatMessage message)
        {
            SubscribeMessage(message);
        }
    }

    private void SubscribeMessage(ObservableChatMessage message)
    {
        if (ReferenceEquals(subscribedMessage, message))
        {
            return;
        }

        subscribedMessage = message;
        message.PropertyChanged += OnMessagePropertyChanged;
        SubscribeContents(message.Contents);
    }

    private void UnsubscribeMessage()
    {
        if (subscribedMessage is null)
        {
            return;
        }

        subscribedMessage.PropertyChanged -= OnMessagePropertyChanged;
        subscribedMessage = null;
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
        if (e.PropertyName == nameof(ObservableChatMessage.Contents) && subscribedMessage is not null)
        {
            UnsubscribeContents();
            SubscribeContents(subscribedMessage.Contents);
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
