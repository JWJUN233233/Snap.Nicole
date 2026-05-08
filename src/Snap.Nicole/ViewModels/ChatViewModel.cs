using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels;

internal sealed partial class ChatViewModel : ObservableObject
{
    private readonly IChatService chatService;
    private readonly IOptionsMonitor<AppSettings> settings;
    private CancellationTokenSource? cts;

    public ChatViewModel(IServiceProvider serviceProvider)
    {
        chatService = serviceProvider.GetRequiredService<IChatService>();
        settings = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
    }

    public ObservableCollection<ExtendedAgentResponseUpdate> Messages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial string InputText { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSendMessage => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        string input = InputText.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        Messages.Add(new ExtendedAgentResponseUpdate
        {
            RoleKind = ChatRoleKind.User,
            Content = input,
        });

        InputText = "";
        IsBusy = true;
        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            ChatCompletionOptions options = new()
            {
                Model = settings.CurrentValue.DefaultModel,
                Temperature = 0.3f,
                TopP = 0.95f,
            };

            string modelId = options.Model;
            await foreach (ExtendedAgentResponseUpdate response in chatService.StreamCompletionAsync(Messages, options, cts.Token))
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
            cts?.Dispose();
            cts = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StopGeneration()
    {
        cts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
    }
}
