using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Sentry;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.AI.Observables;
using Snap.Nicole.Services.Settings;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels;

internal sealed partial class AgentViewModel : ObservableObject, IDisposable
{
    private readonly IAgentService agentService;
    private ObservableSettingsCollection<ModelProviderProfile, Guid>? modelProviderProfiles;
    private ObservableSettingsCollection<ModelProfile, Guid>? modelProfiles;

    private CancellationTokenSource? generationCts;
    private AgentSession? session;
    private bool disposed;

    public AgentViewModel(IServiceProvider serviceProvider)
    {
        agentService = serviceProvider.GetRequiredService<IAgentService>();
        Settings = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;

        Settings.PropertyChanged += OnSettingsPropertyChanged;
        SubscribeModelProviderProfiles(Settings.ModelProviderProfiles);
        SubscribeModelProfiles(Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles);
    }

    public AppSettings Settings { get; }

    public ObservableChatMessageCollection Messages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSendMessage
    {
        get
        {
            ModelProfile? modelProfile = Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles.CurrentItem;
            return !disposed
                && !IsBusy
                && !string.IsNullOrWhiteSpace(InputText)
                && !string.IsNullOrWhiteSpace(modelProfile?.ModelId);
        }
    }

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

        ModelProfile? modelProfile = providerProfile.ModelProfiles.CurrentItem;
        if (modelProfile is null || string.IsNullOrWhiteSpace(modelProfile.ModelId))
        {
            return;
        }

        ExtendedAgentOptions requestOptions = new()
        {
            ProviderType = providerProfile.ProviderType.Value,
            ModelId = modelProfile.ModelId.Trim(),
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

        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ai.chat.send", "Send chat message");
        span.SetTag("ai.provider", requestOptions.ProviderType.ToString());
        span.SetTag("ai.model", requestOptions.ModelId);

        InputText = string.Empty;
        IsBusy = true;
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        generationCts = linkedCts;

        try
        {
            SpanStatus result = await agentService.RunStreamingAsync(userMessage, Messages, requestOptions, session, App.Current.Threading.TaskScheduler, linkedCts.Token);
            span.Finish(result);
        }
        catch (OperationCanceledException)
        {
            span.Finish(SpanStatus.Cancelled);
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ai.chat.send");
            throw;
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

        SentryDiagnostics.AddBreadcrumb("Stop chat generation", "ai.chat", "ui");
        generationCts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        if (disposed)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Clear chat", "ai.chat", "ui");
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
        UnsubscribeModelProfiles();

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
            SubscribeModelProfiles(Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles);
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
            SubscribeModelProfiles(Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles);
            ResetSession();
        }
    }

    private void OnModelProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(ObservableSettingsCollection<,>.CurrentItem) or nameof(ObservableSettingsCollection<,>.CurrentItemId))
        {
            ResetSession();
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

    private void SubscribeModelProfiles(ObservableSettingsCollection<ModelProfile, Guid>? profiles)
    {
        if (disposed || ReferenceEquals(modelProfiles, profiles))
        {
            return;
        }

        UnsubscribeModelProfiles();
        modelProfiles = profiles;
        if (modelProfiles is not null)
        {
            (modelProfiles as INotifyPropertyChanged).PropertyChanged += OnModelProfilesPropertyChanged;
        }
    }

    private void UnsubscribeModelProfiles()
    {
        (modelProfiles as INotifyPropertyChanged)?.PropertyChanged -= OnModelProfilesPropertyChanged;
        modelProfiles = null;
    }

    private void ResetSession()
    {
        session = null;
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern ChatClientAgentSession ChatClientAgentSessionCreate();
}
