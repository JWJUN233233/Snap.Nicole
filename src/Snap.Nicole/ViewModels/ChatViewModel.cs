using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using Snap.Nicole.Services.Settings;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels;

internal sealed partial class ChatViewModel : ObservableObject, IDisposable
{
    private readonly IAgentService chatService;
    private ObservableSettingsCollection<ModelProviderProfile, Guid>? modelProviderProfiles;

    private CancellationTokenSource? generationCts;
    private AgentSession? session;
    private bool disposed;

    public ChatViewModel(IServiceProvider serviceProvider)
    {
        chatService = serviceProvider.GetRequiredService<IAgentService>();
        Settings = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;

        Settings.PropertyChanged += OnSettingsPropertyChanged;
        SubscribeModelProviderProfiles(Settings.ModelProviderProfiles);
    }

    public AppSettings Settings { get; }

    public ObservableChatMessageCollection Messages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSendMessage { get => !disposed && !IsBusy && !string.IsNullOrWhiteSpace(InputText) && Settings.ModelProviderProfiles.CurrentItem is not null; }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        if (disposed)
        {
            return;
        }

        string input = InputText.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        ModelProviderProfile? providerProfile = Settings.ModelProviderProfiles.CurrentItem;
        if (providerProfile is null)
        {
            return;
        }

        ExtendedAgentOptions requestOptions = new()
        {
            ProviderType = providerProfile.ProviderType.Value,
            ModelId = providerProfile.ModelId,
            Endpoint = providerProfile.Endpoint,
            ApiKey = providerProfile.ApiKey,
            Temperature = 0.3f,
            TopP = 0.95f,
            ThinkingEnabled = true,
            OmitReasoningEffortWhenThinkingDisabled = true,
        };

        session ??= ChatClientAgentSessionCreate();

        ChatMessage userMessage = new(ChatRole.User, input)
        {
            CreatedAt = DateTimeOffset.Now,
            AuthorName = "You",
        };

        InputText = string.Empty;
        IsBusy = true;
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        generationCts = linkedCts;

        try
        {
            await chatService.RunStreamingAsync(userMessage, Messages, requestOptions, session, App.Current.Threading.TaskScheduler, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(generationCts, linkedCts))
            {
                generationCts = null;
            }

            linkedCts.Dispose();
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StopGeneration()
    {
        if (disposed)
        {
            return;
        }

        generationCts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        if (disposed)
        {
            return;
        }

        Messages.Clear();
        session = null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        Settings.PropertyChanged -= OnSettingsPropertyChanged;
        UnsubscribeModelProviderProfiles();

        generationCts?.Cancel();
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(AppSettings.ModelProviderProfiles))
        {
            SubscribeModelProviderProfiles(Settings.ModelProviderProfiles);
        }
    }

    private void OnModelProviderProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(ObservableSettingsCollection<,>.CurrentItem) or nameof(ObservableSettingsCollection<,>.CurrentItemId))
        {
            session = null;
            SendMessageCommand.NotifyCanExecuteChanged();
        }
    }

    private void SubscribeModelProviderProfiles(ObservableSettingsCollection<ModelProviderProfile, Guid> providerProfiles)
    {
        if (disposed)
        {
            return;
        }

        if (ReferenceEquals(modelProviderProfiles, providerProfiles))
        {
            return;
        }

        UnsubscribeModelProviderProfiles();
        modelProviderProfiles = providerProfiles;
        (modelProviderProfiles as INotifyPropertyChanged).PropertyChanged += OnModelProviderProfilesPropertyChanged;
    }

    private void UnsubscribeModelProviderProfiles()
    {
        (modelProviderProfiles as INotifyPropertyChanged)?.PropertyChanged -= OnModelProviderProfilesPropertyChanged;
        modelProviderProfiles = null;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern ChatClientAgentSession ChatClientAgentSessionCreate();
}
