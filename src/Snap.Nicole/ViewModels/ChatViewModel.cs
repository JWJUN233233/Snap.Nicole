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

internal sealed partial class ChatViewModel : ObservableObject
{
    private readonly IAgentService chatService;
    private ObservableSettingsCollection<ModelProfile, Guid>? modelProfiles;

    private CancellationTokenSource? generationCts;
    private AgentSession? session;

    public ChatViewModel(IServiceProvider serviceProvider)
    {
        chatService = serviceProvider.GetRequiredService<IAgentService>();
        Settings = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;

        Settings.PropertyChanged += OnSettingsPropertyChanged;
        SubscribeModelProfiles(Settings.ModelProfiles);
    }

    public AppSettings Settings { get; }

    public ObservableChatMessageCollection Messages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string InputText { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSendMessage => !IsBusy && !string.IsNullOrWhiteSpace(InputText) && Settings.ModelProfiles.CurrentItem is not null;

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        string input = InputText.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        ModelProfile? profile = Settings.ModelProfiles.CurrentItem;
        if (profile is null)
        {
            return;
        }

        ExtendedAgentOptions requestOptions = new()
        {
            ProviderType = profile.ProviderType.Value,
            Model = profile.ModelId,
            Endpoint = profile.Endpoint,
            ApiKey = profile.ApiKey,
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

        InputText = "";
        IsBusy = true;
        generationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await chatService.RunStreamingAsync(userMessage, Messages, requestOptions, session, App.Current.Threading.TaskScheduler, generationCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            generationCts?.Dispose();
            generationCts = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StopGeneration()
    {
        generationCts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
        session = null;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppSettings.ModelProfiles))
        {
            SubscribeModelProfiles(Settings.ModelProfiles);
        }
    }

    private void OnModelProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ObservableSettingsCollection<ModelProfile, Guid>.CurrentItem)
            or nameof(ObservableSettingsCollection<ModelProfile, Guid>.CurrentItemId))
        {
            session = null;
            SendMessageCommand.NotifyCanExecuteChanged();
        }
    }

    private void SubscribeModelProfiles(ObservableSettingsCollection<ModelProfile, Guid> profiles)
    {
        if (ReferenceEquals(modelProfiles, profiles))
        {
            return;
        }

        if (modelProfiles is not null)
        {
            ((INotifyPropertyChanged)modelProfiles).PropertyChanged -= OnModelProfilesPropertyChanged;
        }

        modelProfiles = profiles;
        ((INotifyPropertyChanged)modelProfiles).PropertyChanged += OnModelProfilesPropertyChanged;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern ChatClientAgentSession ChatClientAgentSessionCreate();
}
