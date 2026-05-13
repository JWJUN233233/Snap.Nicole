using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels;

internal sealed partial class ChatViewModel : ObservableObject
{
    private readonly IAgentService chatService;
    private readonly IOptionsProvider<AppSettings> options;

    private CancellationTokenSource? generationCts;
    private AgentSession? session;

    public ChatViewModel(IServiceProvider serviceProvider)
    {
        chatService = serviceProvider.GetRequiredService<IAgentService>();
        options = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>();

        options.OnChange(OnSettingsChanged);
    }

    public ObservableCollection<ExtendedAgentResponseUpdate> Messages { get; } = [];

    public IReadOnlyList<ModelProfile> ModelProfiles => options.CurrentValue.ModelProfiles.AsReadOnly();

    public ModelProfile? SelectedModelProfile
    {
        get
        {
            AppSettings current = options.CurrentValue;
            return current.ModelProfiles.FirstOrDefault(p => p.Id == current.SelectedModelProfileId)
                ?? current.ModelProfiles.FirstOrDefault();
        }
        set
        {
            if (value is null)
            {
                return;
            }

            AppSettings current = options.CurrentValue;
            if (current.SelectedModelProfileId == value.Id)
            {
                return;
            }

            current.SelectedModelProfileId = value.Id;
            options.Update();
            session = null;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string InputText { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSendMessage => !IsBusy && !string.IsNullOrWhiteSpace(InputText) && SelectedModelProfile is not null;

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        string input = InputText.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        ModelProfile? profile = SelectedModelProfile;
        if (profile is null)
        {
            return;
        }

        ExtendedAgentOptions requestOptions = new()
        {
            Model = profile.ModelId,
            Endpoint = profile.Endpoint,
            ApiKey = profile.ApiKey,
            Temperature = 0.3f,
            TopP = 0.95f,
            ThinkingEnabled = true,
            OmitReasoningEffortWhenThinkingDisabled = true,
        };

        session ??= chatService.CreateSession(requestOptions);

        ExtendedAgentResponseUpdate userMessage = new()
        {
            RoleKind = ChatRoleKind.User,
            Content = input,
        };

        Messages.Add(userMessage);

        InputText = "";
        IsBusy = true;
        generationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            string modelId = requestOptions.Model;
            await foreach (ExtendedAgentResponseUpdate response in chatService.RunStreamingAsync(userMessage, requestOptions, session, generationCts.Token))
            {
                ExtendedAgentResponseUpdate tagged = new()
                {
                    RoleKind = response.RoleKind,
                    Content = response.Content,
                    Segments = response.Segments,
                    Timestamp = response.Timestamp,
                    ModelId = modelId,
                };

                if (Messages.Count > 0 && Messages[^1].RoleKind == ChatRoleKind.Assistant)
                {
                    Messages[^1] = tagged;
                }
                else
                {
                    Messages.Add(tagged);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Messages.Add(new ExtendedAgentResponseUpdate
            {
                RoleKind = ChatRoleKind.Assistant,
                Content = $"Error: {ex.Message}",
            });
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

    private void OnSettingsChanged(AppSettings ignored, string? ignored2)
    {
        App.Current.Threading.SynchronizationContext.Post(static state =>
        {
            if (state is not ChatViewModel self)
            {
                return;
            }

            self.OnPropertyChanged(nameof(ModelProfiles));
            self.OnPropertyChanged(nameof(SelectedModelProfile));
        }, this);
    }
}
